import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import api from '../services/api';
import Layout from '../components/Layout';
import ExpenseModal from '../components/ExpenseModal';
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
  X
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
  const [activeTab, setActiveTab] = useState<'expenses' | 'settlements' | 'history'>('expenses');

  // Modals
  const [isExpenseModalOpen, setIsExpenseModalOpen] = useState(false);
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

  useEffect(() => {
    fetchGroupDetails();
  }, [groupId]);

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
                          <div key={expense.id} className="p-5 rounded-2xl bg-slate-950/20 border border-white/5 flex flex-col md:flex-row md:items-center justify-between space-y-4 md:space-y-0 group relative">
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
                                onClick={() => handleDeleteExpense(expense.id)}
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
                        // Xác định xem user có phải người nhận (creditor) của tx này không
                        // Đối sánh dựa trên nickname của member liên kết
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
