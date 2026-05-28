import React, { useEffect, useMemo, useState } from 'react';
import api from '../services/api';
import { X, Image as ImageIcon } from 'lucide-react';

interface ExpenseDetailModalProps {
  isOpen: boolean;
  onClose: () => void;
  expenseId: string | null;
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

interface ExpenseDetail {
  id: string;
  groupId: string;
  description: string;
  totalAmount: number;
  splitMethod: string;
  imageUrl?: string | null;
  category?: string | null;
  createdById?: string | null;
  createdAt: string;
  payers: ExpensePayer[];
  slices: ExpenseSlice[];
}

const ExpenseDetailModal: React.FC<ExpenseDetailModalProps> = ({ isOpen, onClose, expenseId }) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [detail, setDetail] = useState<ExpenseDetail | null>(null);

  const cacheBustedImageUrl = useMemo(() => {
    if (!detail?.imageUrl) return '';
    const url = detail.imageUrl;
    return `${url}${url.includes('?') ? '&' : '?'}v=${Date.now()}`;
  }, [detail?.imageUrl]);

  useEffect(() => {
    const fetchDetail = async () => {
      if (!isOpen || !expenseId) return;
      setLoading(true);
      setError('');
      setDetail(null);
      try {
        const res = await api.get(`/expenses/${expenseId}`);
        setDetail(res.data);
      } catch (err: any) {
        setError(err.response?.data?.message || 'Không thể tải chi tiết hóa đơn.');
      } finally {
        setLoading(false);
      }
    };

    fetchDetail();
  }, [isOpen, expenseId]);

  const formatCurrency = (val: number) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
  };

  if (!isOpen) return null;

  const dateStr = detail?.createdAt
    ? new Date(detail.createdAt).toLocaleString('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      })
    : '';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 overflow-y-auto">
      <div className="fixed inset-0 bg-black/60 backdrop-blur-sm" onClick={onClose}></div>

      <div className="w-full max-w-2xl bg-[#0f172a] border border-white/10 rounded-3xl p-6 md:p-8 shadow-2xl relative z-10 max-h-[90vh] overflow-y-auto animate-in fade-in zoom-in-95 duration-200">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 p-2 text-slate-400 hover:text-slate-200 rounded-lg hover:bg-white/5 transition-all"
          aria-label="Đóng"
        >
          <X size={20} />
        </button>

        <h2 className="text-xl md:text-2xl font-extrabold text-white mb-2 flex items-center gap-2">
          <ImageIcon className="text-indigo-400" size={20} />
          Chi tiết hóa đơn
        </h2>
        <p className="text-xs text-slate-500 mb-6">Xem đầy đủ thông tin chia tiền và ảnh bill (nếu có).</p>

        {error && (
          <div className="mb-5 p-4 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-400 text-sm">
            {error}
          </div>
        )}

        {loading && (
          <div className="py-10 text-center text-slate-400 text-sm">
            <div className="inline-block w-6 h-6 border-2 border-indigo-500 border-t-transparent rounded-full animate-spin mr-2"></div>
            <span>Đang tải chi tiết...</span>
          </div>
        )}

        {!loading && detail && (
          <div className="space-y-6">
            <div className="p-5 rounded-2xl bg-slate-900/40 border border-white/5 space-y-2">
              <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-2">
                <div>
                  <p className="text-white font-bold text-base">{detail.description}</p>
                  <p className="text-[11px] text-slate-500 mt-0.5">Ngày tạo: {dateStr}</p>
                </div>
                <div className="text-right">
                  <p className="text-[10px] text-slate-500 uppercase tracking-wide">Tổng hóa đơn</p>
                  <p className="text-2xl font-extrabold text-white">{formatCurrency(detail.totalAmount)}</p>
                </div>
              </div>

              <div className="flex flex-wrap gap-2 pt-2">
                <span className="text-[10px] bg-indigo-500/10 text-indigo-300 px-2 py-0.5 rounded-full font-semibold">
                  Split: {detail.splitMethod}
                </span>
                {detail.category && (
                  <span className="text-[10px] bg-slate-800 text-slate-300 px-2 py-0.5 rounded-full font-semibold">
                    Danh mục: {detail.category}
                  </span>
                )}
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="p-5 rounded-2xl bg-slate-900/30 border border-white/5">
                <p className="text-xs font-bold text-white mb-3">Ai trả tiền?</p>
                {detail.payers?.length ? (
                  <div className="space-y-2">
                    {detail.payers.map((p) => (
                      <div key={`${p.memberId}`} className="flex justify-between items-center text-xs">
                        <span className="text-slate-300">{p.nickname}</span>
                        <span className="text-emerald-400 font-bold">{formatCurrency(p.amountPaid)}</span>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-xs text-slate-500">Không có dữ liệu người trả.</p>
                )}
              </div>

              <div className="p-5 rounded-2xl bg-slate-900/30 border border-white/5">
                <p className="text-xs font-bold text-white mb-3">Ai nợ bao nhiêu?</p>
                {detail.slices?.length ? (
                  <div className="space-y-2">
                    {detail.slices.map((s) => (
                      <div key={`${s.memberId}`} className="flex justify-between items-center text-xs">
                        <span className="text-slate-300">{s.nickname}</span>
                        <span className="text-indigo-300 font-bold">{formatCurrency(s.amountOwed)}</span>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-xs text-slate-500">Không có dữ liệu nợ.</p>
                )}
              </div>
            </div>

            <div className="p-5 rounded-2xl bg-slate-900/30 border border-white/5">
              <p className="text-xs font-bold text-white mb-3">Ảnh hóa đơn</p>
              {detail.imageUrl ? (
                <a
                  href={detail.imageUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="block rounded-2xl overflow-hidden border border-white/10 bg-slate-950/30 hover:border-indigo-500/40 transition-colors"
                  title="Mở ảnh hóa đơn"
                >
                  <img src={cacheBustedImageUrl} alt="Ảnh hóa đơn" className="w-full max-h-[360px] object-contain bg-black/30" />
                </a>
              ) : (
                <p className="text-xs text-slate-500">Hóa đơn này chưa có ảnh.</p>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default ExpenseDetailModal;

