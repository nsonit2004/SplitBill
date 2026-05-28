import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import api from '../services/api';
import Layout from '../components/Layout';
import { 
  Plus, 
  ArrowUpRight, 
  ArrowDownLeft, 
  Wallet, 
  ChevronRight, 
  X,
  Sparkles,
  Users
} from 'lucide-react';

interface Group {
  groupId: string;
  name: string;
  description?: string;
  totalSpent: number;
  userNetBalance: number;
  createdAt: string;
}

const Dashboard: React.FC = () => {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [groups, setGroups] = useState<Group[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Stats
  const [totalSpent, setTotalSpent] = useState(0);
  const [totalOwed, setTotalOwed] = useState(0);
  const [totalReceivable, setTotalReceivable] = useState(0);

  // Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [groupName, setGroupName] = useState('');
  const [groupDesc, setGroupDesc] = useState('');
  const [memberNamesInput, setMemberNamesInput] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const fetchGroups = async () => {
    try {
      setLoading(true);
      const response = await api.get('/groups');
      
      const mappedGroups = response.data.map((g: any) => ({
        groupId: g.id,
        name: g.name,
        description: g.description,
        totalSpent: g.totalSpent || 0,
        userNetBalance: g.userNetBalance || 0,
        createdAt: g.createdAt
      }));
      
      setGroups(mappedGroups);

      // Tính toán stats tổng hợp
      let spent = 0;
      let owed = 0;
      let receivable = 0;

      mappedGroups.forEach((g: Group) => {
        spent += g.totalSpent;
        if (g.userNetBalance < 0) {
          owed += Math.abs(g.userNetBalance);
        } else if (g.userNetBalance > 0) {
          receivable += g.userNetBalance;
        }
      });

      setTotalSpent(spent);
      setTotalOwed(owed);
      setTotalReceivable(receivable);
    } catch (err: any) {
      setError(err.response?.data?.message || t('common.error'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchGroups();
  }, []);

  const handleCreateGroup = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!groupName.trim()) return;

    setSubmitting(true);
    setError('');

    // Xử lý chuỗi tên thành viên cách nhau bởi dấu phẩy
    const guests = memberNamesInput
      .split(',')
      .map(name => name.trim())
      .filter(name => name.length > 0);

    try {
      await api.post('/groups', {
        name: groupName,
        description: groupDesc,
        members: guests
      });

      // Reset form
      setGroupName('');
      setGroupDesc('');
      setMemberNamesInput('');
      setIsModalOpen(false);

      // Refresh list
      await fetchGroups();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Không thể tạo nhóm.');
    } finally {
      setSubmitting(false);
    }
  };

  const formatCurrency = (val: number) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
  };

  return (
    <Layout>
      <div className="space-y-8 max-w-7xl mx-auto">
        {/* Header Dashboard */}
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center space-y-4 sm:space-y-0">
          <div>
            <h1 className="text-3xl font-extrabold text-white flex items-center space-x-2">
              <Sparkles className="text-indigo-400" size={28} />
              <span>{t('dashboard.title')}</span>
            </h1>
            <p className="text-slate-400 text-sm mt-1">Quản lý chi tiêu nhóm tối ưu hóa nợ của bạn.</p>
          </div>
          <button
            onClick={() => setIsModalOpen(true)}
            className="flex items-center space-x-2 py-3 px-6 rounded-xl bg-gradient-to-r from-[#6366f1] to-[#4f46e5] hover:from-indigo-500 hover:to-indigo-600 text-sm font-bold shadow-lg shadow-indigo-500/20 active:scale-95 transition-all"
          >
            <Plus size={18} />
            <span>{t('dashboard.create_group')}</span>
          </button>
        </div>

        {/* Stats Cards Section */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {/* Card 1: Tổng tiêu nhóm */}
          <div className="p-6 rounded-2xl bg-slate-900/40 border border-white/5 backdrop-blur-xl relative overflow-hidden flex items-center justify-between">
            <div className="space-y-2">
              <p className="text-xs text-slate-400 uppercase tracking-wider font-semibold">{t('dashboard.total_spent')}</p>
              <p className="text-2xl font-bold text-white">{formatCurrency(totalSpent)}</p>
            </div>
            <div className="w-12 h-12 rounded-xl bg-slate-800/80 flex items-center justify-center text-slate-400 border border-white/5">
              <Wallet size={22} />
            </div>
          </div>

          {/* Card 2: Bạn nợ */}
          <div className="p-6 rounded-2xl bg-slate-900/40 border border-white/5 backdrop-blur-xl relative overflow-hidden flex items-center justify-between group">
            <div className="absolute top-0 right-0 w-24 h-24 bg-rose-500/5 rounded-full blur-2xl pointer-events-none"></div>
            <div className="space-y-2">
              <p className="text-xs text-slate-400 uppercase tracking-wider font-semibold">{t('dashboard.you_owe')}</p>
              <p className="text-2xl font-bold text-rose-400">{formatCurrency(totalOwed)}</p>
            </div>
            <div className="w-12 h-12 rounded-xl bg-rose-500/10 flex items-center justify-center text-rose-400 border border-rose-500/20">
              <ArrowDownLeft size={22} />
            </div>
          </div>

          {/* Card 3: Bạn được nhận */}
          <div className="p-6 rounded-2xl bg-slate-900/40 border border-white/5 backdrop-blur-xl relative overflow-hidden flex items-center justify-between group">
            <div className="absolute top-0 right-0 w-24 h-24 bg-emerald-500/5 rounded-full blur-2xl pointer-events-none"></div>
            <div className="space-y-2">
              <p className="text-xs text-slate-400 uppercase tracking-wider font-semibold">{t('dashboard.you_are_owed')}</p>
              <p className="text-2xl font-bold text-emerald-400">{formatCurrency(totalReceivable)}</p>
            </div>
            <div className="w-12 h-12 rounded-xl bg-emerald-500/10 flex items-center justify-center text-emerald-400 border border-emerald-500/20">
              <ArrowUpRight size={22} />
            </div>
          </div>
        </div>

        {error && (
          <div className="p-4 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-400 text-sm">
            {error}
          </div>
        )}

        {/* Groups Grid Section */}
        <div className="space-y-4">
          <h2 className="text-xl font-bold text-white">{t('dashboard.your_groups')}</h2>

          {loading ? (
            <div className="text-center py-12 text-slate-400 text-sm">
              <div className="inline-block w-6 h-6 border-2 border-indigo-500 border-t-transparent rounded-full animate-spin mr-2"></div>
              <span>{t('common.loading')}</span>
            </div>
          ) : groups.length === 0 ? (
            <div className="text-center py-16 bg-slate-900/20 rounded-3xl border border-white/5 p-8 flex flex-col items-center">
              <Users size={48} className="text-slate-600 mb-4" />
              <p className="text-slate-400 text-sm">{t('dashboard.no_groups')}</p>
              <button
                onClick={() => setIsModalOpen(true)}
                className="mt-4 text-xs font-semibold text-indigo-400 hover:underline"
              >
                + {t('dashboard.create_group')}
              </button>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {groups.map((group) => (
                <div
                  key={group.groupId}
                  onClick={() => navigate(`/groups/${group.groupId}`)}
                  className="p-6 rounded-2xl bg-[#0f172a]/60 hover:bg-slate-900/60 border border-white/5 hover:border-white/10 shadow-xl transition-all duration-200 cursor-pointer flex flex-col justify-between h-52 group relative overflow-hidden"
                >
                  <div className="space-y-3">
                    <div className="flex justify-between items-start">
                      <h3 className="font-bold text-lg text-white group-hover:text-indigo-400 transition-colors truncate max-w-[80%]">
                        {group.name}
                      </h3>
                      <ChevronRight size={18} className="text-slate-500 group-hover:text-indigo-400 transition-all translate-x-0 group-hover:translate-x-1" />
                    </div>
                    <p className="text-slate-400 text-xs line-clamp-2 leading-relaxed">
                      {group.description || 'Không có mô tả.'}
                    </p>
                  </div>

                  <div className="pt-4 border-t border-white/5 flex justify-between items-center text-xs">
                    <span className="text-slate-400">
                      Tổng chi: <strong className="text-slate-300 font-semibold">{formatCurrency(group.totalSpent)}</strong>
                    </span>

                    {group.userNetBalance === 0 ? (
                      <span className="px-2.5 py-1 rounded-full bg-slate-800 text-slate-400 font-medium">Hòa nợ</span>
                    ) : group.userNetBalance > 0 ? (
                      <span className="px-2.5 py-1 rounded-full bg-emerald-500/10 text-emerald-400 font-medium">
                        +{formatCurrency(group.userNetBalance)}
                      </span>
                    ) : (
                      <span className="px-2.5 py-1 rounded-full bg-rose-500/10 text-rose-400 font-medium">
                        -{formatCurrency(Math.abs(group.userNetBalance))}
                      </span>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Modal Tạo Nhóm */}
        {isModalOpen && (
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="fixed inset-0 bg-black/60 backdrop-blur-sm" onClick={() => setIsModalOpen(false)}></div>
            
            <div className="w-full max-w-lg bg-[#0f172a] border border-white/10 rounded-3xl p-6 md:p-8 shadow-2xl relative z-10 animate-in fade-in zoom-in-95 duration-200">
              <button
                onClick={() => setIsModalOpen(false)}
                className="absolute top-4 right-4 p-2 text-slate-400 hover:text-slate-200 rounded-lg hover:bg-white/5 transition-all"
              >
                <X size={20} />
              </button>

              <h2 className="text-2xl font-extrabold text-white mb-6">
                {t('dashboard.create_group')}
              </h2>

              <form onSubmit={handleCreateGroup} className="space-y-6">
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider">{t('dashboard.group_name')}</label>
                  <input
                    type="text"
                    required
                    placeholder="Ví dụ: Du lịch Phú Quốc 2026"
                    value={groupName}
                    onChange={(e) => setGroupName(e.target.value)}
                    className="w-full px-4 py-3 rounded-xl bg-slate-900 border border-white/5 focus:border-indigo-500 focus:outline-none text-sm text-white"
                  />
                </div>

                <div className="space-y-1.5">
                  <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider">{t('dashboard.group_desc')}</label>
                  <textarea
                    placeholder="Nhập mô tả ngắn cho nhóm..."
                    value={groupDesc}
                    onChange={(e) => setGroupDesc(e.target.value)}
                    rows={2}
                    className="w-full px-4 py-3 rounded-xl bg-slate-900 border border-white/5 focus:border-indigo-500 focus:outline-none text-sm text-white resize-none"
                  />
                </div>

                <div className="space-y-1.5">
                  <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
                    {t('dashboard.add_members')}
                  </label>
                  <input
                    type="text"
                    placeholder="Hải, Quân, Linh, Vân..."
                    value={memberNamesInput}
                    onChange={(e) => setMemberNamesInput(e.target.value)}
                    className="w-full px-4 py-3 rounded-xl bg-slate-900 border border-white/5 focus:border-indigo-500 focus:outline-none text-sm text-white"
                  />
                </div>

                <div className="flex space-x-3 pt-4">
                  <button
                    type="button"
                    onClick={() => setIsModalOpen(false)}
                    className="flex-1 py-3 rounded-xl bg-white/5 hover:bg-white/10 text-sm font-bold text-slate-300 transition-colors"
                  >
                    {t('common.cancel')}
                  </button>
                  <button
                    type="submit"
                    disabled={submitting || !groupName.trim()}
                    className="flex-1 py-3 rounded-xl bg-gradient-to-r from-indigo-600 to-indigo-500 hover:from-indigo-500 hover:to-indigo-600 text-sm font-bold shadow-lg shadow-indigo-500/20 transition-all disabled:opacity-50"
                  >
                    {submitting ? t('common.loading') : t('common.save')}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
      </div>
    </Layout>
  );
};

export default Dashboard;
