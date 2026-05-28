import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import api from '../services/api';
import Layout from '../components/Layout';
import ExpenseModal from '../components/ExpenseModal';
import ExpenseDetailModal from '../components/ExpenseDetailModal';
import SettleModal from '../components/SettleModal';
import { useAuth } from '../context/AuthContext';
import { 
  Plus, 
  ArrowLeft, 
  Coins, 
  Trash2, 
  FileText, 
  History, 
  Link as LinkIcon, 
  Share2,
  Copy,
  CheckCircle, 
  Clock, 
  Image as ImageIcon,
  X,
  BarChart2,
  TrendingUp
} from 'lucide-react';

interface GroupMember {
  id: string;
  nickname: string;
  isVirtual: boolean;
  userId?: string;
  bankCode?: string;
  bankAccountNo?: string;
  bankAccountName?: string;
}

interface ExpensePayer {
  memberId: string;
  nickname: string;
  amountPaid: number;
}

interface ExpenseSlice {
  memberId: string;
  nickname: string;
  amountOwed: number;
}

interface Expense {
  id: string;
  description: string;
  totalAmount: number;
  splitMethod: string;
  imageUrl?: string;
  createdById: string;
  createdAt: string;
  payers: ExpensePayer[];
  slices: ExpenseSlice[];
}

interface SimplifiedDebt {
  debtorId: string;
  debtorName: string;
  creditorId: string;
  creditorName: string;
  amount: number;
  vietQrUrl?: string;
  bankCode?: string;
  bankAccountNo?: string;
  bankAccountName?: string;
}

interface SettleTransaction {
  id: string;
  debtorId: string;
  debtorName: string;
  creditorId: string;
  creditorName: string;
  amount: number;
  paymentMethod: string;
  status: 'Suggested' | 'Pending' | 'Completed' | 'Cancelled';
  transferReference?: string;
  proofImageUrl?: string;
  vietQrUrl?: string;
  createdAt: string;
}

interface GroupInvite {
  inviteToken: string;
  groupId: string;
  groupName: string;
  maxUses: number;
  usedCount: number;
  isRevoked: boolean;
  createdAt: string;
  expiresAt: string;
}

const GroupDetailPage: React.FC = () => {
  const { groupId } = useParams<{ groupId: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { user } = useAuth();

  const [group, setGroup] = useState<any>(null);
  const [members, setMembers] = useState<GroupMember[]>([]);
  const [expenses, setExpenses] = useState<Expense[]>([]);
  const [simplifiedDebts, setSimplifiedDebts] = useState<SimplifiedDebt[]>([]);
  const [historyTransactions, setHistoryTransactions] = useState<SettleTransaction[]>([]);
  const [netBalances, setNetBalances] = useState<Record<string, number>>({});
  const [invites, setInvites] = useState<GroupInvite[]>([]);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [activeTab, setActiveTab] = useState<'expenses' | 'settlements' | 'history' | 'analytics'>('expenses');

  // Analytics State
  const [analytics, setAnalytics] = useState<{
    totalSpending: number;
    totalExpenses: number;
    categoryBreakdown: Array<{ category: string; amount: number; count: number; percentage: number }>;
    topSpenders: Array<{ memberId: string; nickname: string; amountOwed: number }>;
  } | null>(null);
  const [analyticsLoading, setAnalyticsLoading] = useState(false);
  const [hoveredCategory, setHoveredCategory] = useState<string | null>(null);

  // Modals
  const [isExpenseModalOpen, setIsExpenseModalOpen] = useState(false);
  const [isExpenseDetailOpen, setIsExpenseDetailOpen] = useState(false);
  const [selectedExpenseId, setSelectedExpenseId] = useState<string | null>(null);
  const [isSettleModalOpen, setIsSettleModalOpen] = useState(false);
  const [selectedDebt, setSelectedDebt] = useState<SimplifiedDebt | null>(null);

  // Add Member State
  const [isAddMemberOpen, setIsAddMemberOpen] = useState(false);
  const [newMemberName, setNewMemberName] = useState('');
  const [creatingInvite, setCreatingInvite] = useState(false);
  const [inviteLink, setInviteLink] = useState('');
  
  // Link Member State
  const [linkingMemberId, setLinkingMemberId] = useState<string | null>(null);
  const [linkUserEmail, setLinkUserEmail] = useState('');
  const [nudgingId, setNudgingId] = useState<string | null>(null);

  const fetchGroupDetails = async (silent = false) => {
    if (!groupId) return;
    try {
      if (!silent) {
        setLoading(true);
      }
      setError('');

      // 1. Lấy chi tiết nhóm
      const groupResponse = await api.get(`/groups/${groupId}`);
      setGroup(groupResponse.data);
      setMembers((groupResponse.data.members || []).map((m: any) => ({
        ...m,
        userId: m.userId ?? m.linkedUserId
      })));

      // 2. Lấy danh sách hóa đơn
      const expensesResponse = await api.get(`/expenses/group/${groupId}`);
      setExpenses(expensesResponse.data);

      // 3. Lấy số dư Net Balance thực tế
      const balancesResponse = await api.get(`/settlements/group/${groupId}/balances`);
      const balMap: Record<string, number> = {};
      balancesResponse.data.forEach((b: any) => {
        balMap[b.memberId] = b.netBalance;
      });
      setNetBalances(balMap);

      // 4. Lấy danh sách nợ rút gọn
      const simplifiedResponse = await api.get(`/settlements/group/${groupId}/simplified`);
      setSimplifiedDebts(
        (simplifiedResponse.data || []).map((d: any) => ({
          debtorId: d.debtorId,
          debtorName: d.debtorName ?? d.debtorNickname ?? 'Unknown',
          creditorId: d.creditorId,
          creditorName: d.creditorName ?? d.creditorNickname ?? 'Unknown',
          amount: d.amount,
          vietQrUrl: d.vietQrUrl
        }))
      );

      // 5. Lấy lịch sử giao dịch trả nợ
      const historyResponse = await api.get(`/settlements/group/${groupId}/history`);
      setHistoryTransactions(
        (historyResponse.data || []).map((tx: any) => ({
          id: tx.id,
          debtorId: tx.debtorId,
          debtorName: tx.debtorName ?? tx.debtorNickname ?? 'Unknown',
          creditorId: tx.creditorId,
          creditorName: tx.creditorName ?? tx.creditorNickname ?? 'Unknown',
          amount: tx.amount,
          paymentMethod: tx.paymentMethod,
          status: tx.status ?? tx.paymentStatus ?? 'Pending',
          transferReference: tx.transferReference,
          proofImageUrl: tx.proofImageUrl,
          vietQrUrl: tx.vietQrUrl,
          createdAt: tx.createdAt
        }))
      );

      // 6. Lấy danh sách lời mời
      const invitesResponse = await api.get(`/groups/${groupId}/invites`);
      setInvites(invitesResponse.data || []);

    } catch (err: any) {
      setError(err.response?.data?.message || 'Không thể lấy thông tin chi tiết nhóm.');
    } finally {
      if (!silent) {
        setLoading(false);
      }
    }
  };

  const fetchAnalytics = async () => {
    if (!groupId) return;
    setAnalyticsLoading(true);
    try {
      const res = await api.get(`/groups/${groupId}/analytics`);
      setAnalytics(res.data);
    } catch {
      // ignore analytics errors silently
    } finally {
      setAnalyticsLoading(false);
    }
  };

  useEffect(() => {
    fetchGroupDetails();
  }, [groupId]);

  useEffect(() => {
    if (activeTab === 'analytics') {
      fetchAnalytics();
    }
  }, [activeTab, groupId]);

  useEffect(() => {
    if (!groupId) return;

    const intervalId = window.setInterval(() => {
      if (document.visibilityState === 'visible') {
        fetchGroupDetails(true);
      }
    }, 5000);

    return () => window.clearInterval(intervalId);
  }, [groupId]);

  const handleAddMemberSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newMemberName.trim() || !groupId) return;

    try {
      await api.post(`/groups/${groupId}/members`, `"${newMemberName}"`, {
        headers: { 'Content-Type': 'application/json' }
      });
      setNewMemberName('');
      setIsAddMemberOpen(false);
      await fetchGroupDetails();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Không thể thêm thành viên.');
    }
  };

  const handleLinkUserSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const normalizedEmail = linkUserEmail.trim();
    if (!normalizedEmail || !linkingMemberId || !groupId) return;

    try {
      await api.post(`/groups/${groupId}/members/${linkingMemberId}/link`, null, {
        params: { userEmail: normalizedEmail }
      });
      setLinkUserEmail('');
      setLinkingMemberId(null);
      await fetchGroupDetails();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Không thể liên kết tài khoản.');
    }
  };

  const handleDeleteExpense = async (expenseId: string) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa hóa đơn này không? Số nợ sẽ được tính toán lại.')) return;
    try {
      await api.delete(`/expenses/${expenseId}`);
      await fetchGroupDetails();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Không thể xóa hóa đơn.');
    }
  };

  const handleCreateInviteLink = async () => {
    if (!groupId) return;
    try {
      setCreatingInvite(true);
      const response = await api.post(`/groups/${groupId}/invites`, {
        expiresInHours: 72,
        maxUses: 25
      });
      const token = response.data?.inviteToken;
      if (!token) {
        setError('Không thể tạo link mời lúc này.');
        return;
      }
      const link = `${window.location.origin}/join/${token}`;
      setInviteLink(link);
      await fetchGroupDetails();
      setError('');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Không thể tạo link mời.');
    } finally {
      setCreatingInvite(false);
    }
  };

  const handleCopyInviteLink = async () => {
    if (!inviteLink) return;
    try {
      await navigator.clipboard.writeText(inviteLink);
    } catch {
      setError('Không thể copy link. Vui lòng copy thủ công.');
    }
  };

  const handleCopyInviteFromToken = async (token: string) => {
    const link = `${window.location.origin}/join/${token}`;
    try {
      await navigator.clipboard.writeText(link);
    } catch {
      setError('Không thể copy link. Vui lòng copy thủ công.');
    }
  };

  const handleRevokeInvite = async (token: string) => {
    if (!groupId) return;
    try {
      await api.post(`/groups/${groupId}/invites/${token}/revoke`);
      await fetchGroupDetails();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Không thể thu hồi link mời.');
    }
  };

  const handleApproveSettle = async (transactionId: string) => {
    try {
      await api.post(`/settlements/transactions/${transactionId}/complete`);
      await fetchGroupDetails();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Không thể duyệt thanh toán.');
    }
  };

  const handleNudge = async (debt: SimplifiedDebt) => {
    if (!groupId) return;
    const key = `${debt.debtorId}-${debt.creditorId}`;
    setNudgingId(key);
    try {
      await api.post(`/settlements/group/${groupId}/nudge`, {
        debtorId: debt.debtorId,
        creditorId: debt.creditorId,
        amount: debt.amount
      });
      alert(`Đã gửi nhắc nợ thành công tới ${debt.debtorName}!`);
    } catch (err: any) {
      alert(err.response?.data?.message || 'Không thể gửi nhắc nợ. Có thể tài khoản Guest chưa liên kết email.');
    } finally {
      setNudgingId(null);
    }
  };

  const formatCurrency = (val: number) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
  };

  const currentMember = members.find((m) => m.userId === user?.userId);
  const payableDebts = currentMember
    ? simplifiedDebts.filter((debt) => debt.debtorId === currentMember.id)
    : [];

  return (
    <Layout>
      <div className="max-w-7xl mx-auto space-y-8">
        {/* Nút Back */}
        <button
          onClick={() => navigate('/dashboard')}
          className="flex items-center space-x-2 text-slate-400 hover:text-white transition-colors text-sm"
        >
          <ArrowLeft size={16} />
          <span>Quay lại Dashboard</span>
        </button>

        {loading && !group ? (
          <div className="text-center py-12 text-slate-400 text-sm">
            <div className="inline-block w-6 h-6 border-2 border-indigo-500 border-t-transparent rounded-full animate-spin mr-2"></div>
            <span>{t('common.loading')}</span>
          </div>
        ) : (
          <>
            {error && (
              <div className="mb-6 p-4 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-400 text-sm">
                {error}
              </div>
            )}
            <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
            
            {/* Cột 1: Thông tin nhóm & Thành viên (Cột bên trái - 4/12 cols) */}
            <div className="lg:col-span-4 space-y-6">
              <div className="p-6 rounded-3xl bg-slate-900/40 border border-white/5 backdrop-blur-xl space-y-6">
                <div>
                  <h1 className="text-2xl font-extrabold text-white truncate">{group?.name}</h1>
                  <p className="text-slate-400 text-xs mt-1 leading-relaxed">{group?.description || 'Không có mô tả cho nhóm này.'}</p>
                </div>

                {/* Danh sách thành viên */}
                <div className="space-y-4">
                  <div className="flex justify-between items-center">
                    <h2 className="text-sm font-bold uppercase tracking-wider text-slate-400">{t('group.members')}</h2>
                    <div className="flex items-center space-x-3">
                      <button
                        onClick={handleCreateInviteLink}
                        disabled={creatingInvite}
                        className="text-xs font-semibold text-emerald-400 hover:underline flex items-center space-x-1 disabled:opacity-60"
                      >
                        <Share2 size={14} />
                        <span>{creatingInvite ? 'Đang tạo...' : 'Mời thành viên'}</span>
                      </button>
                      <button
                        onClick={() => setIsAddMemberOpen(true)}
                        className="text-xs font-semibold text-indigo-400 hover:underline flex items-center space-x-1"
                      >
                        <Plus size={14} />
                        <span>{t('group.add_member')}</span>
                      </button>
                    </div>
                  </div>

                  {inviteLink && (
                    <div className="rounded-xl border border-emerald-500/20 bg-emerald-500/5 p-3">
                      <p className="mb-2 text-[10px] uppercase tracking-wide text-emerald-400">Link mời tham gia nhóm</p>
                      <div className="flex items-center space-x-2">
                        <input
                          readOnly
                          value={inviteLink}
                          className="w-full rounded-lg border border-white/10 bg-slate-900 px-2 py-1.5 text-[11px] text-slate-300"
                        />
                        <button
                          onClick={handleCopyInviteLink}
                          className="inline-flex items-center space-x-1 rounded-lg bg-emerald-600 px-2.5 py-1.5 text-[10px] font-semibold text-white hover:bg-emerald-500"
                        >
                          <Copy size={12} />
                          <span>Copy</span>
                        </button>
                      </div>
                    </div>
                  )}

                  {invites.length > 0 && (
                    <div className="space-y-2">
                      <p className="text-[10px] uppercase tracking-wide text-slate-500">Danh sách link mời</p>
                      <div className="space-y-2">
                        {invites.map((invite) => {
                          const isExpired = new Date(invite.expiresAt).getTime() <= Date.now();
                          const isExhausted = invite.usedCount >= invite.maxUses;
                          const isInactive = invite.isRevoked || isExpired || isExhausted;
                          return (
                            <div key={invite.inviteToken} className="rounded-xl border border-white/10 bg-slate-900/30 p-2.5">
                              <div className="flex items-center justify-between space-x-2">
                                <div className="text-[10px] text-slate-400">
                                  <p>
                                    {invite.usedCount}/{invite.maxUses} lượt
                                    {invite.isRevoked ? ' - Đã thu hồi' : isExpired ? ' - Hết hạn' : isExhausted ? ' - Hết lượt' : ' - Còn hiệu lực'}
                                  </p>
                                  <p>Hết hạn: {new Date(invite.expiresAt).toLocaleString('vi-VN')}</p>
                                </div>
                                <div className="flex items-center space-x-1.5">
                                  <button
                                    onClick={() => handleCopyInviteFromToken(invite.inviteToken)}
                                    className="inline-flex items-center space-x-1 rounded-md bg-indigo-600/20 px-2 py-1 text-[10px] text-indigo-300 hover:bg-indigo-600/30"
                                  >
                                    <Copy size={10} />
                                    <span>Copy</span>
                                  </button>
                                  {!invite.isRevoked && (
                                    <button
                                      onClick={() => handleRevokeInvite(invite.inviteToken)}
                                      disabled={isInactive}
                                      className="rounded-md bg-rose-600/20 px-2 py-1 text-[10px] text-rose-300 hover:bg-rose-600/30 disabled:opacity-50"
                                    >
                                      Thu hồi
                                    </button>
                                  )}
                                </div>
                              </div>
                            </div>
                          );
                        })}
                      </div>
                    </div>
                  )}

                  <div className="space-y-3">
                    {members.map(m => {
                      const bal = netBalances[m.id] || 0;
                      return (
                        <div key={m.id} className="flex justify-between items-center p-3 rounded-2xl bg-slate-950/20 border border-white/5">
                          <div className="flex items-center space-x-3 truncate">
                            <div className="w-8 h-8 rounded-full bg-slate-800 flex items-center justify-center font-bold text-slate-400 text-xs">
                              {m.nickname.substring(0, 2).toUpperCase()}
                            </div>
                            <div className="truncate text-xs font-semibold">
                              <p className="text-white truncate flex items-center space-x-1">
                                <span>{m.nickname}</span>
                                {m.isVirtual ? (
                                  <span className="text-[9px] bg-slate-800 text-slate-400 px-1.5 py-0.5 rounded-full font-medium">Guest</span>
                                ) : (
                                  <span className="text-[9px] bg-indigo-500/10 text-indigo-400 px-1.5 py-0.5 rounded-full font-medium">User</span>
                                )}
                              </p>
                              {m.isVirtual && (
                                <button
                                  onClick={() => setLinkingMemberId(m.id)}
                                  className="text-[10px] text-indigo-400 hover:underline flex items-center space-x-0.5 mt-0.5"
                                >
                                  <LinkIcon size={10} />
                                  <span>{t('common.link_account')}</span>
                                </button>
                              )}
                            </div>
                          </div>

                          <div className="text-right text-xs">
                            {bal === 0 ? (
                              <span className="text-slate-500 font-medium">Hòa nợ</span>
                            ) : bal > 0 ? (
                              <span className="text-emerald-400 font-bold">+{formatCurrency(bal)}</span>
                            ) : (
                              <span className="text-rose-400 font-bold">-{formatCurrency(Math.abs(bal))}</span>
                            )}
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </div>
              </div>
            </div>

            {/* Cột 2: Nội dung chính (Hóa đơn / Trả nợ / Lịch sử - 8/12 cols) */}
            <div className="lg:col-span-8 space-y-6">
              
              {/* Tabs Navigation */}
              <div className="flex space-x-1 p-1 bg-slate-900/60 border border-white/5 rounded-2xl backdrop-blur-xl">
                <button
                  onClick={() => setActiveTab('expenses')}
                  className={`flex-1 flex items-center justify-center space-x-2 py-3 rounded-xl text-xs font-bold transition-all ${
                    activeTab === 'expenses'
                      ? 'bg-indigo-600/10 text-indigo-400 border border-indigo-500/20'
                      : 'text-slate-400 hover:text-slate-200'
                  }`}
                >
                  <FileText size={16} />
                  <span>{t('group.expenses')} ({expenses.length})</span>
                </button>

                <button
                  onClick={() => setActiveTab('settlements')}
                  className={`flex-1 flex items-center justify-center space-x-2 py-3 rounded-xl text-xs font-bold transition-all ${
                    activeTab === 'settlements'
                      ? 'bg-indigo-600/10 text-indigo-400 border border-indigo-500/20'
                      : 'text-slate-400 hover:text-slate-200'
                  }`}
                >
                  <Coins size={16} />
                  <span>{t('group.settlements')} ({payableDebts.length})</span>
                </button>

                <button
                  onClick={() => setActiveTab('history')}
                  className={`flex-1 flex items-center justify-center space-x-2 py-3 rounded-xl text-xs font-bold transition-all ${
                    activeTab === 'history'
                      ? 'bg-indigo-600/10 text-indigo-400 border border-indigo-500/20'
                      : 'text-slate-400 hover:text-slate-200'
                  }`}
                >
                  <History size={16} />
                  <span>Lịch sử ({historyTransactions.length})</span>
                </button>

                <button
                  onClick={() => setActiveTab('analytics')}
                  className={`flex-1 flex items-center justify-center space-x-2 py-3 rounded-xl text-xs font-bold transition-all ${
                    activeTab === 'analytics'
                      ? 'bg-indigo-600/10 text-indigo-400 border border-indigo-500/20'
                      : 'text-slate-400 hover:text-slate-200'
                  }`}
                >
                  <BarChart2 size={16} />
                  <span>Phân tích</span>
                </button>
              </div>

              {/* Tab 1: Expenses View */}
              {activeTab === 'expenses' && (
                <div className="p-6 rounded-3xl bg-slate-900/40 border border-white/5 backdrop-blur-xl space-y-6">
                  <div className="flex justify-between items-center">
                    <h2 className="text-lg font-bold text-white">Danh sách hóa đơn</h2>
                    <button
                      onClick={() => setIsExpenseModalOpen(true)}
                      className="flex items-center space-x-1.5 py-2.5 px-4 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-xs font-bold shadow-lg shadow-indigo-500/20 transition-all active:scale-95"
                    >
                      <Plus size={14} />
                      <span>{t('group.add_expense')}</span>
                    </button>
                  </div>

                  {expenses.length === 0 ? (
                    <div className="text-center py-16 flex flex-col items-center">
                      <FileText size={48} className="text-slate-600 mb-3" />
                      <p className="text-slate-400 text-xs">{t('group.no_expenses')}</p>
                    </div>
                  ) : (
                    <div className="space-y-4">
                      {expenses.map((expense) => {
                        const dateStr = new Date(expense.createdAt).toLocaleDateString('vi-VN', {
                          day: '2-digit',
                          month: '2-digit',
                          year: 'numeric'
                        });
                        return (
                          <div
                            key={expense.id}
                            className="p-5 rounded-2xl bg-slate-950/20 border border-white/5 flex flex-col md:flex-row md:items-center justify-between space-y-4 md:space-y-0 group relative cursor-pointer hover:border-indigo-500/20 hover:bg-slate-950/30 transition-colors"
                            onClick={() => {
                              setSelectedExpenseId(expense.id);
                              setIsExpenseDetailOpen(true);
                            }}
                            role="button"
                            tabIndex={0}
                            onKeyDown={(e) => {
                              if (e.key === 'Enter' || e.key === ' ') {
                                setSelectedExpenseId(expense.id);
                                setIsExpenseDetailOpen(true);
                              }
                            }}
                          >
                            <div className="space-y-1">
                              <h3 className="font-bold text-sm text-white">{expense.description}</h3>
                              <p className="text-[11px] text-slate-500">
                                Ngày: {dateStr} | Người trả:{' '}
                                <strong className="text-slate-400 font-semibold">
                                  {expense.payers.map(p => `${p.nickname} (${formatCurrency(p.amountPaid)})`).join(', ')}
                                </strong>
                              </p>
                              <div className="flex flex-wrap gap-1.5 mt-2">
                                <span className="text-[9px] bg-slate-800 text-slate-400 px-2 py-0.5 rounded-full font-medium">
                                  {t(`group.split_${expense.splitMethod.toLowerCase()}`)}
                                </span>
                                {expense.imageUrl && (
                                  <a 
                                    href={expense.imageUrl} 
                                    target="_blank" 
                                    rel="noreferrer"
                                    onClick={(e) => e.stopPropagation()}
                                    className="text-[9px] bg-indigo-500/10 text-indigo-400 px-2 py-0.5 rounded-full font-medium flex items-center space-x-1 hover:bg-indigo-500/20"
                                  >
                                    <ImageIcon size={10} />
                                    <span>Hóa đơn</span>
                                  </a>
                                )}
                              </div>
                            </div>

                            <div className="flex items-center space-x-4">
                              <span className="text-base font-extrabold text-white">{formatCurrency(expense.totalAmount)}</span>
                              <button
                                onClick={(e) => {
                                  e.stopPropagation();
                                  handleDeleteExpense(expense.id);
                                }}
                                className="p-2 text-rose-500 hover:bg-rose-500/10 rounded-lg transition-colors md:opacity-0 group-hover:opacity-100"
                                title="Xóa hóa đơn"
                              >
                                <Trash2 size={16} />
                              </button>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  )}
                </div>
              )}

              {/* Tab 2: Suggested Payments (Settlements) View */}
              {activeTab === 'settlements' && (
                <div className="p-6 rounded-3xl bg-slate-900/40 border border-white/5 backdrop-blur-xl space-y-6">
                  <h2 className="text-lg font-bold text-white">{t('group.suggested_settlements')}</h2>

                  {simplifiedDebts.length === 0 ? (
                    <div className="text-center py-16 flex flex-col items-center">
                      <CheckCircle size={48} className="text-emerald-500 mb-3" />
                      <p className="text-slate-400 text-xs">{t('group.no_settlements')}</p>
                    </div>
                  ) : (
                    <div className="space-y-4">
                      {simplifiedDebts.map((debt, idx) => {
                        const isDebtor = currentMember && debt.debtorId === currentMember.id;
                        const isCreditor = currentMember && debt.creditorId === currentMember.id;
                        const nudgeKey = `${debt.debtorId}-${debt.creditorId}`;
                        const isNudging = nudgingId === nudgeKey;

                        return (
                          <div key={idx} className="p-4 rounded-2xl bg-slate-950/20 border border-white/5 flex items-center justify-between">
                            <div className="text-xs space-y-1">
                              <p className="text-slate-300">
                                <strong className="text-rose-400 font-semibold">{debt.debtorName}</strong> nợ{' '}
                                <strong className="text-emerald-400 font-semibold">{debt.creditorName}</strong>
                              </p>
                              <p className="text-[10px] text-slate-500">
                                Số tiền:{' '}
                                <strong className="text-slate-300 font-semibold">{formatCurrency(debt.amount)}</strong>
                              </p>
                            </div>

                            <div className="flex items-center space-x-2">
                              {isDebtor && (
                                <button
                                  onClick={() => {
                                    setSelectedDebt(debt);
                                    setIsSettleModalOpen(true);
                                  }}
                                  className="py-2 px-4 rounded-xl bg-emerald-600 hover:bg-emerald-500 text-xs font-bold text-white shadow-md active:scale-95 transition-all"
                                >
                                  {t('group.pay')}
                                </button>
                              )}
                              {isCreditor && (
                                <button
                                  onClick={() => handleNudge(debt)}
                                  disabled={isNudging}
                                  className="py-2 px-4 rounded-xl bg-indigo-600 hover:bg-indigo-500 disabled:bg-slate-800 text-xs font-bold text-white shadow-md active:scale-95 transition-all"
                                >
                                  {isNudging ? 'Đang gửi...' : 'Nhắc nợ'}
                                </button>
                              )}
                              {!isDebtor && !isCreditor && (
                                <span className="text-[10px] text-slate-500 italic bg-white/5 px-2.5 py-1 rounded-lg">Đang nợ</span>
                              )}
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  )}
                </div>
              )}

              {/* Tab 3: Payment History View */}
              {activeTab === 'history' && (
                <div className="p-6 rounded-3xl bg-slate-900/40 border border-white/5 backdrop-blur-xl space-y-6">
                  <h2 className="text-lg font-bold text-white">Lịch sử thanh toán nợ</h2>

                  {historyTransactions.length === 0 ? (
                    <div className="text-center py-16 text-slate-500 text-xs">
                      Không có giao dịch thanh toán nợ nào được thực hiện.
                    </div>
                  ) : (
                    <div className="space-y-4">
                      {historyTransactions.map((tx) => {
                        const isReceiver = members.find(m => m.id === tx.creditorId)?.userId === user?.userId;

                        return (
                          <div key={tx.id} className="p-4 rounded-2xl bg-[#0f172a]/60 border border-white/5 flex flex-col sm:flex-row sm:items-center justify-between space-y-4 sm:space-y-0">
                            <div className="space-y-1">
                              <p className="text-xs text-slate-300">
                                <strong className="text-slate-200">{tx.debtorName}</strong> →{' '}
                                <strong className="text-slate-200">{tx.creditorName}</strong>
                              </p>
                              <div className="flex items-center space-x-2 text-[10px] text-slate-500">
                                <span>Tiền: {formatCurrency(tx.amount)}</span>
                                <span>|</span>
                                <span>{tx.paymentMethod}</span>
                                {tx.transferReference && (
                                  <>
                                    <span>|</span>
                                    <span>Ref: {tx.transferReference}</span>
                                  </>
                                )}
                                {tx.proofImageUrl && (
                                  <>
                                    <span>|</span>
                                    <a
                                      href={tx.proofImageUrl}
                                      target="_blank"
                                      rel="noreferrer"
                                      className="text-indigo-400 hover:underline flex items-center space-x-0.5"
                                    >
                                      <ImageIcon size={10} />
                                      <span>Xem ảnh bill</span>
                                    </a>
                                  </>
                                )}
                              </div>
                            </div>

                            <div className="flex items-center space-x-3">
                              {tx.status === 'Pending' ? (
                                <div className="flex items-center space-x-2">
                                  <span className="inline-flex items-center space-x-1 px-2.5 py-1 rounded-full bg-amber-500/10 text-amber-400 text-[10px] font-medium">
                                    <Clock size={10} />
                                    <span>Đang chờ duyệt</span>
                                  </span>

                                  {isReceiver && (
                                    <button
                                      onClick={() => handleApproveSettle(tx.id)}
                                      className="py-1.5 px-3 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-[10px] font-bold shadow-md transition-colors"
                                    >
                                      Duyệt nhận tiền
                                    </button>
                                  )}
                                </div>
                              ) : tx.status === 'Cancelled' ? (
                                <span className="inline-flex items-center space-x-1 px-2.5 py-1 rounded-full bg-slate-500/10 text-slate-300 text-[10px] font-medium">
                                  <X size={10} />
                                  <span>Đã hủy</span>
                                </span>
                              ) : (
                                <span className="inline-flex items-center space-x-1 px-2.5 py-1 rounded-full bg-emerald-500/10 text-emerald-400 text-[10px] font-medium">
                                  <CheckCircle size={10} />
                                  <span>Đã hoàn tất</span>
                                </span>
                              )}
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  )}
                </div>
              )}

              {/* Tab 4: Analytics View */}
              {activeTab === 'analytics' && (() => {
                const CATEGORY_COLORS: Record<string, { fill: string; text: string; bg: string }> = {
                  Food:          { fill: '#6366f1', text: 'text-indigo-400',  bg: 'bg-indigo-500/10' },
                  Transport:     { fill: '#22d3ee', text: 'text-cyan-400',    bg: 'bg-cyan-500/10' },
                  Accommodation: { fill: '#a78bfa', text: 'text-violet-400',  bg: 'bg-violet-500/10' },
                  Entertainment: { fill: '#fb923c', text: 'text-orange-400',  bg: 'bg-orange-500/10' },
                  Shopping:      { fill: '#34d399', text: 'text-emerald-400', bg: 'bg-emerald-500/10' },
                  Other:         { fill: '#94a3b8', text: 'text-slate-400',   bg: 'bg-slate-500/10' },
                };
                const CATEGORY_ICONS: Record<string, string> = {
                  Food: '🍜', Transport: '🚗', Accommodation: '🏨',
                  Entertainment: '🎉', Shopping: '🛍️', Other: '📦',
                };

                // Build SVG Donut Chart
                const buildDonut = (items: Array<{ category: string; amount: number; percentage: number }>) => {
                  if (!items || items.length === 0) return null;
                  const R = 70; const cx = 90; const cy = 90;
                  const circumference = 2 * Math.PI * R;
                  let accumulated = 0;
                  const segments = items.map((item) => {
                    const offset = circumference - (item.percentage / 100) * circumference;
                    const rotation = (accumulated / 100) * 360 - 90;
                    accumulated += item.percentage;
                    return { ...item, offset, rotation };
                  });
                  return (
                    <svg viewBox="0 0 180 180" className="w-full h-full drop-shadow-2xl">
                      <defs>
                        <filter id="glow">
                          <feGaussianBlur stdDeviation="3" result="coloredBlur"/>
                          <feMerge><feMergeNode in="coloredBlur"/><feMergeNode in="SourceGraphic"/></feMerge>
                        </filter>
                      </defs>
                      {/* Background ring */}
                      <circle cx={cx} cy={cy} r={R} fill="none" stroke="#1e293b" strokeWidth="28" />
                      {segments.map((seg, i) => {
                        const isHovered = hoveredCategory === seg.category;
                        return (
                          <circle
                            key={i}
                            cx={cx} cy={cy} r={R}
                            fill="none"
                            stroke={CATEGORY_COLORS[seg.category]?.fill || '#94a3b8'}
                            strokeWidth={isHovered ? 32 : 26}
                            strokeDasharray={`${circumference} ${circumference}`}
                            strokeDashoffset={seg.offset}
                            strokeLinecap="round"
                            transform={`rotate(${seg.rotation}, ${cx}, ${cy})`}
                            style={{ transition: 'stroke-width 0.25s ease, filter 0.25s ease', cursor: 'pointer', filter: isHovered ? 'url(#glow)' : 'none', opacity: hoveredCategory && !isHovered ? 0.4 : 1 }}
                            onMouseEnter={() => setHoveredCategory(seg.category)}
                            onMouseLeave={() => setHoveredCategory(null)}
                          />
                        );
                      })}
                      {/* Center text */}
                      {hoveredCategory ? (
                        <>
                          <text x={cx} y={cy - 10} textAnchor="middle" fill="white" fontSize="11" fontWeight="700">
                            {CATEGORY_ICONS[hoveredCategory] || '📦'} {hoveredCategory}
                          </text>
                          <text x={cx} y={cy + 8} textAnchor="middle" fill="#a5b4fc" fontSize="13" fontWeight="800">
                            {(segments.find(s => s.category === hoveredCategory)?.percentage || 0).toFixed(1)}%
                          </text>
                        </>
                      ) : (
                        <>
                          <text x={cx} y={cy - 8} textAnchor="middle" fill="#94a3b8" fontSize="9" fontWeight="500">Tổng chi tiêu</text>
                          <text x={cx} y={cy + 10} textAnchor="middle" fill="white" fontSize="10" fontWeight="800">
                            {analytics ? (analytics.totalSpending / 1000).toFixed(0) + 'K' : '—'}
                          </text>
                        </>
                      )}
                    </svg>
                  );
                };

                return (
                  <div className="space-y-6">
                    {analyticsLoading ? (
                      <div className="p-12 rounded-3xl bg-slate-900/40 border border-white/5 flex items-center justify-center">
                        <div className="inline-block w-6 h-6 border-2 border-indigo-500 border-t-transparent rounded-full animate-spin mr-3" />
                        <span className="text-slate-400 text-sm">Đang tải phân tích...</span>
                      </div>
                    ) : !analytics || analytics.totalExpenses === 0 ? (
                      <div className="p-12 rounded-3xl bg-slate-900/40 border border-white/5 text-center">
                        <BarChart2 size={48} className="text-slate-600 mx-auto mb-3" />
                        <p className="text-slate-400 text-sm">Chưa có dữ liệu chi tiêu để phân tích.</p>
                        <p className="text-slate-500 text-xs mt-1">Thêm hóa đơn đầu tiên để xem biểu đồ phân tích.</p>
                      </div>
                    ) : (
                      <>
                        {/* Summary Cards */}
                        <div className="grid grid-cols-2 gap-4">
                          <div className="p-5 rounded-2xl bg-gradient-to-br from-indigo-600/10 to-indigo-900/20 border border-indigo-500/20">
                            <p className="text-[10px] text-indigo-400 font-semibold uppercase tracking-wider mb-1">Tổng chi tiêu nhóm</p>
                            <p className="text-2xl font-extrabold text-white">{formatCurrency(analytics.totalSpending)}</p>
                            <p className="text-[10px] text-slate-500 mt-1">từ {analytics.totalExpenses} hóa đơn</p>
                          </div>
                          <div className="p-5 rounded-2xl bg-gradient-to-br from-emerald-600/10 to-emerald-900/20 border border-emerald-500/20">
                            <p className="text-[10px] text-emerald-400 font-semibold uppercase tracking-wider mb-1">Trung bình/hóa đơn</p>
                            <p className="text-2xl font-extrabold text-white">
                              {formatCurrency(analytics.totalExpenses > 0 ? analytics.totalSpending / analytics.totalExpenses : 0)}
                            </p>
                            <p className="text-[10px] text-slate-500 mt-1">mỗi lần chi tiêu</p>
                          </div>
                        </div>

                        {/* Donut Chart + Category Breakdown */}
                        <div className="p-6 rounded-3xl bg-slate-900/40 border border-white/5 backdrop-blur-xl">
                          <div className="flex items-center space-x-2 mb-5">
                            <TrendingUp size={16} className="text-indigo-400" />
                            <h3 className="text-sm font-bold text-white">Phân tích danh mục chi tiêu</h3>
                          </div>
                          <div className="flex flex-col md:flex-row gap-6 items-center">
                            {/* SVG Donut */}
                            <div className="w-44 h-44 flex-shrink-0 relative">
                              {buildDonut(analytics.categoryBreakdown)}
                            </div>

                            {/* Legend & Breakdown */}
                            <div className="flex-1 space-y-2.5 w-full">
                              {analytics.categoryBreakdown.map((cat) => {
                                const colors = CATEGORY_COLORS[cat.category] || CATEGORY_COLORS['Other'];
                                const isHov = hoveredCategory === cat.category;
                                return (
                                  <div
                                    key={cat.category}
                                    className={`flex items-center justify-between p-3 rounded-xl border transition-all cursor-pointer ${
                                      isHov
                                        ? `${colors.bg} border-current/30`
                                        : 'bg-slate-950/30 border-white/5 hover:bg-slate-900/60'
                                    }`}
                                    onMouseEnter={() => setHoveredCategory(cat.category)}
                                    onMouseLeave={() => setHoveredCategory(null)}
                                  >
                                    <div className="flex items-center space-x-3">
                                      <span className="text-base">{CATEGORY_ICONS[cat.category] || '📦'}</span>
                                      <div>
                                        <p className={`text-xs font-semibold ${isHov ? colors.text : 'text-slate-300'}`}>{cat.category}</p>
                                        <p className="text-[10px] text-slate-500">{cat.count} hóa đơn</p>
                                      </div>
                                    </div>
                                    <div className="text-right">
                                      <p className="text-xs font-bold text-white">{formatCurrency(cat.amount)}</p>
                                      <div className="flex items-center space-x-1 justify-end mt-1">
                                        <div className="h-1 rounded-full" style={{ width: `${Math.max(cat.percentage, 4)}px`, backgroundColor: CATEGORY_COLORS[cat.category]?.fill || '#94a3b8' }} />
                                        <span className={`text-[10px] font-bold ${colors.text}`}>{cat.percentage.toFixed(1)}%</span>
                                      </div>
                                    </div>
                                  </div>
                                );
                              })}
                            </div>
                          </div>
                        </div>

                        {/* Top Spenders */}
                        {analytics.topSpenders.length > 0 && (
                          <div className="p-6 rounded-3xl bg-slate-900/40 border border-white/5 backdrop-blur-xl">
                            <div className="flex items-center space-x-2 mb-5">
                              <TrendingUp size={16} className="text-orange-400" />
                              <h3 className="text-sm font-bold text-white">Bảng xếp hạng chi tiêu</h3>
                            </div>
                            <div className="space-y-3">
                              {analytics.topSpenders.map((spender, idx) => {
                                const maxAmount = analytics.topSpenders[0]?.amountOwed || 1;
                                const barWidth = (spender.amountOwed / maxAmount) * 100;
                                const medals = ['🥇', '🥈', '🥉'];
                                return (
                                  <div key={spender.memberId} className="flex items-center space-x-3">
                                    <span className="text-base w-6 text-center flex-shrink-0">
                                      {medals[idx] || `#${idx + 1}`}
                                    </span>
                                    <div className="flex-1 min-w-0">
                                      <div className="flex justify-between items-center mb-1">
                                        <span className="text-xs font-semibold text-slate-300 truncate">{spender.nickname}</span>
                                        <span className="text-xs font-bold text-white ml-2">{formatCurrency(spender.amountOwed)}</span>
                                      </div>
                                      <div className="w-full h-1.5 bg-slate-800 rounded-full overflow-hidden">
                                        <div
                                          className="h-full rounded-full transition-all duration-700"
                                          style={{
                                            width: `${barWidth}%`,
                                            background: idx === 0 ? '#fbbf24' : idx === 1 ? '#94a3b8' : idx === 2 ? '#c2763f' : '#6366f1'
                                          }}
                                        />
                                      </div>
                                    </div>
                                  </div>
                                );
                              })}
                            </div>
                          </div>
                        )}
                      </>
                    )}
                  </div>
                );
              })()}
            </div>
          </div>
        </>
      )}

        {/* Modal Thêm Thành Viên Guest */}
        {isAddMemberOpen && (
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="fixed inset-0 bg-black/60 backdrop-blur-sm" onClick={() => setIsAddMemberOpen(false)}></div>
            <div className="w-full max-w-sm bg-[#0f172a] border border-white/10 rounded-3xl p-6 shadow-2xl relative z-10 animate-in fade-in zoom-in-95 duration-200">
              <button
                onClick={() => setIsAddMemberOpen(false)}
                className="absolute top-4 right-4 p-2 text-slate-400 hover:text-slate-200 rounded-lg hover:bg-white/5"
              >
                <X size={20} />
              </button>
              <h3 className="text-lg font-bold text-white mb-4">{t('group.add_member')}</h3>
              <form onSubmit={handleAddMemberSubmit} className="space-y-4">
                <input
                  type="text"
                  required
                  placeholder="Nhập biệt danh thành viên..."
                  value={newMemberName}
                  onChange={(e) => setNewMemberName(e.target.value)}
                  className="w-full px-4 py-3 rounded-xl bg-slate-900 border border-white/5 focus:border-indigo-500 focus:outline-none text-xs text-white"
                />
                <button
                  type="submit"
                  className="w-full py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-xs font-bold"
                >
                  Thêm thành viên
                </button>
              </form>
            </div>
          </div>
        )}

        {/* Modal Liên kết Tài khoản real */}
        {linkingMemberId && (
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="fixed inset-0 bg-black/60 backdrop-blur-sm" onClick={() => setLinkingMemberId(null)}></div>
            <div className="w-full max-w-sm bg-[#0f172a] border border-white/10 rounded-3xl p-6 shadow-2xl relative z-10 animate-in fade-in zoom-in-95 duration-200">
              <button
                onClick={() => setLinkingMemberId(null)}
                className="absolute top-4 right-4 p-2 text-slate-400 hover:text-slate-200 rounded-lg hover:bg-white/5"
              >
                <X size={20} />
              </button>
              <h3 className="text-lg font-bold text-white mb-2">Liên kết tài khoản người dùng</h3>
              <p className="text-[10px] text-slate-400 mb-4">
                Nhập Email tài khoản đã đăng ký của thành viên này. Sau khi liên kết, tài khoản Guest này sẽ chuyển sang trạng thái User thực.
              </p>
              <form onSubmit={handleLinkUserSubmit} className="space-y-4">
                <input
                  type="email"
                  required
                  placeholder="Nhập email tài khoản cần liên kết..."
                  value={linkUserEmail}
                  onChange={(e) => setLinkUserEmail(e.target.value)}
                  className="w-full px-4 py-3 rounded-xl bg-slate-900 border border-white/5 focus:border-indigo-500 focus:outline-none text-xs text-white"
                />
                <button
                  type="submit"
                  className="w-full py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-xs font-bold"
                >
                  Liên kết tài khoản
                </button>
              </form>
            </div>
          </div>
        )}

        {/* Expense Modal */}
        <ExpenseModal
          isOpen={isExpenseModalOpen}
          onClose={() => setIsExpenseModalOpen(false)}
          groupId={groupId || ''}
          members={members}
          onSuccess={fetchGroupDetails}
        />

        <ExpenseDetailModal
          isOpen={isExpenseDetailOpen}
          onClose={() => {
            setIsExpenseDetailOpen(false);
            setSelectedExpenseId(null);
          }}
          expenseId={selectedExpenseId}
        />

        {/* Settle Modal (Dynamic VietQR) */}
        {selectedDebt && (
          <SettleModal
            isOpen={isSettleModalOpen}
            onClose={() => {
              setIsSettleModalOpen(false);
              setSelectedDebt(null);
            }}
            groupId={groupId || ''}
            debtorId={selectedDebt.debtorId}
            debtorName={selectedDebt.debtorName}
            creditorId={selectedDebt.creditorId}
            creditorName={selectedDebt.creditorName}
            amount={selectedDebt.amount}
            vietQrUrl={selectedDebt.vietQrUrl}
            bankCode={selectedDebt.bankCode}
            bankAccountNo={selectedDebt.bankAccountNo}
            bankAccountName={selectedDebt.bankAccountName}
            onSuccess={fetchGroupDetails}
          />
        )}
      </div>
    </Layout>
  );
};

export default GroupDetailPage;
