import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import api from '../services/api';
import { X, Upload, Check, QrCode, AlertCircle } from 'lucide-react';

const inflightCreateTxRequests = new Map<string, Promise<any>>();

const POPULAR_MOBILE_BANKS = [
  { id: 'vcb', name: 'Vietcombank', logo: 'VCB' },
  { id: 'tcb', name: 'Techcombank', logo: 'TCB' },
  { id: 'mb', name: 'MB Bank', logo: 'MB' },
  { id: 'bidv', name: 'BIDV', logo: 'BIDV' },
  { id: 'vietin', name: 'VietinBank', logo: 'CTG' },
  { id: 'vpbank', name: 'VPBank', logo: 'VPB' },
  { id: 'acb', name: 'ACB', logo: 'ACB' }
];

interface SettleModalProps {
  isOpen: boolean;
  onClose: () => void;
  groupId: string;
  debtorId: string;
  debtorName: string;
  creditorId: string;
  creditorName: string;
  amount: number;
  vietQrUrl?: string;
  // Thông tin ngân hàng của chủ nợ để sinh VietQR
  bankCode?: string;
  bankAccountNo?: string;
  bankAccountName?: string;
  onSuccess: () => void;
}

const SettleModal: React.FC<SettleModalProps> = ({
  isOpen,
  onClose,
  groupId,
  debtorId,
  debtorName,
  creditorId,
  creditorName,
  amount,
  vietQrUrl,
  bankCode,
  bankAccountNo,
  bankAccountName,
  onSuccess
}) => {
  const { t } = useTranslation();
  const translate = t;

  const [paymentMethod, setPaymentMethod] = useState<'VietQR' | 'Cash'>('VietQR');
  const [proofUrl, setProofUrl] = useState('');
  const [proofPreviewUrl, setProofPreviewUrl] = useState('');
  const [uploading, setUploading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [transactionId, setTransactionId] = useState<string | null>(null);
  const [transferReference, setTransferReference] = useState<string | null>(null);
  const [serverQrUrl, setServerQrUrl] = useState<string>('');
  const [isMobile, setIsMobile] = useState(false);

  useEffect(() => {
    const userAgent = navigator.userAgent || navigator.vendor || (window as any).opera;
    const mobileRegex = /android|iphone|ipad|ipod|blackberry|iemobile|opera mini/i;
    setIsMobile(mobileRegex.test(userAgent));
  }, []);

  // Polling loop to check transaction status
  useEffect(() => {
    if (!transactionId || paymentMethod !== 'VietQR') return;

    const intervalId = window.setInterval(async () => {
      try {
        const response = await api.get(`/settlements/group/${groupId}/history`);
        const txs: any[] = response.data || [];
        const currentTx = txs.find((t) => t.id === transactionId);
        
        if (currentTx && (currentTx.status === 'Completed' || currentTx.paymentStatus === 'Completed')) {
          clearInterval(intervalId);
          onSuccess();
          onClose();
        }
      } catch (err) {
        // ignore errors in polling loop
      }
    }, 3000);

    return () => {
      clearInterval(intervalId);
    };
  }, [transactionId, paymentMethod, groupId, onSuccess, onClose]);

  if (!isOpen) return null;

  // Cú pháp nội dung chuyển khoản tự động
  const memoText = `SB SETTLE ${debtorName.toUpperCase()} TO ${creditorName.toUpperCase()}`
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '') // Bỏ dấu tiếng Việt
    .replace(/[^A-Z0-9 ]/g, '')     // Loại bỏ ký tự đặc biệt
    .substring(0, 25);               // Giới hạn độ dài

  // Tạo URL ảnh VietQR động
  // mb | vcb | tcb...
  const formattedBankCode = (bankCode || '').toLowerCase().replace(/[^a-z0-9]/g, '');
  const generatedQrUrl = formattedBankCode && bankAccountNo
    ? `https://img.vietqr.io/image/${formattedBankCode}-${bankAccountNo}-compact.png?amount=${amount}&addInfo=${encodeURIComponent(memoText)}&accountName=${encodeURIComponent(bankAccountName || '')}`
    : '';
  const qrUrl = serverQrUrl || vietQrUrl || generatedQrUrl;

  useEffect(() => {
    const createTxIfNeeded = async () => {
      if (!isOpen) return;
      if (paymentMethod !== 'VietQR') return;
      if (transactionId) return;

      setSubmitting(true);
      setError('');
      try {
        const requestKey = `${groupId}|${debtorId}|${creditorId}|${amount}|VietQR`;
        let requestPromise = inflightCreateTxRequests.get(requestKey);
        if (!requestPromise) {
          requestPromise = api.post(`/settlements/group/${groupId}/transactions`, {
            debtorId,
            creditorId,
            amount,
            paymentMethod: 'VietQR'
          });
          inflightCreateTxRequests.set(requestKey, requestPromise);
        }

        const response = await requestPromise;
        setTransactionId(response.data?.id || null);
        setTransferReference(response.data?.transferReference || null);
        setServerQrUrl(response.data?.vietQrUrl || '');
      } catch (err: any) {
        setError(err.response?.data?.message || 'Không thể tạo giao dịch thanh toán.');
      } finally {
        const requestKey = `${groupId}|${debtorId}|${creditorId}|${amount}|VietQR`;
        if (inflightCreateTxRequests.has(requestKey)) {
          inflightCreateTxRequests.delete(requestKey);
        }
        setSubmitting(false);
      }
    };

    createTxIfNeeded();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, paymentMethod]);

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setUploading(true);
    setError('');
    setProofUrl('');
    setProofPreviewUrl('');

    const formData = new FormData();
    formData.append('file', file);

    try {
      const response = await api.post('/expenses/upload', formData, {
        headers: {
          'Content-Type': 'multipart/form-data'
        }
      });
      const url = response.data?.imageUrl;
      if (!url || typeof url !== 'string') {
        setError('Upload thành công nhưng không nhận được URL ảnh hợp lệ.');
        return;
      }
      setProofUrl(url);
      setProofPreviewUrl(`${url}${url.includes('?') ? '&' : '?'}v=${Date.now()}`);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Tải ảnh chuyển khoản thất bại. Vui lòng chọn lại.');
    } finally {
      setUploading(false);
    }
  };

  const handleSettleSubmit = async () => {
    try {
      // Nếu Cash thì tạo và auto complete ở backend
      let txId = transactionId;
      if (paymentMethod === 'Cash') {
        setSubmitting(true);
        setError('');
        const response = await api.post(`/settlements/group/${groupId}/transactions`, {
          debtorId,
          creditorId,
          amount,
          paymentMethod: 'Cash'
        });
        txId = response.data?.id;
      }

      // 2. Nếu có ảnh proof, cập nhật proof lên transaction
      if (proofUrl && txId) {
        await api.post(`/settlements/transactions/${txId}/proof`, `"${proofUrl}"`, {
          headers: {
            'Content-Type': 'application/json'
          }
        });
      }

      onSuccess();
      onClose();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Có lỗi xảy ra khi cập nhật thanh toán.');
    } finally {
      setSubmitting(false);
    }
  };

  const formatCurrency = (val: number) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 overflow-y-auto">
      <div
        className="fixed inset-0 bg-black/60 backdrop-blur-sm"
        onClick={async () => {
          // Nếu đã tạo tx VietQR nhưng user đóng modal, hủy để tránh rác Pending
          if (transactionId && paymentMethod === 'VietQR') {
            try {
              await api.post(`/settlements/transactions/${transactionId}/cancel`);
            } catch {
              // ignore
            }
          }
          onClose();
        }}
      ></div>

      <div className="w-full max-w-md bg-[#0f172a] border border-white/10 rounded-3xl p-6 shadow-2xl relative z-10 max-h-[90vh] overflow-y-auto animate-in fade-in zoom-in-95 duration-200">
        <button
          onClick={async () => {
            if (transactionId && paymentMethod === 'VietQR') {
              try {
                await api.post(`/settlements/transactions/${transactionId}/cancel`);
              } catch {
                // ignore
              }
            }
            onClose();
          }}
          className="absolute top-4 right-4 p-2 text-slate-400 hover:text-slate-200 rounded-lg hover:bg-white/5 transition-all"
        >
          <X size={20} />
        </button>

        <h2 className="text-xl font-extrabold text-white mb-6 flex items-center space-x-2">
          <QrCode className="text-indigo-400" size={22} />
          <span>{translate('group.vietqr_title')}</span>
        </h2>

        {error && (
          <div className="mb-6 p-4 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-400 text-xs">
            {error}
          </div>
        )}

        <div className="space-y-6">
          {/* Thông tin chuyển khoản */}
          <div className="p-4 rounded-2xl bg-slate-900/40 border border-white/5 space-y-2">
            <div className="flex justify-between text-xs text-slate-400">
              <span>Người trả:</span>
              <strong className="text-white font-semibold">{debtorName}</strong>
            </div>
            <div className="flex justify-between text-xs text-slate-400">
              <span>Người nhận:</span>
              <strong className="text-white font-semibold">{creditorName}</strong>
            </div>
            <div className="flex justify-between text-xs text-slate-400 border-t border-white/5 pt-2 mt-2">
              <span>Số tiền:</span>
              <strong className="text-emerald-400 font-bold text-base">{formatCurrency(amount)}</strong>
            </div>
          </div>

          {/* Chọn phương thức thanh toán */}
          <div className="flex space-x-2">
            <button
              onClick={() => setPaymentMethod('VietQR')}
              className={`flex-1 py-2 rounded-xl text-xs font-semibold border transition-all ${
                paymentMethod === 'VietQR'
                  ? 'bg-indigo-600/10 border-indigo-500 text-indigo-400'
                  : 'bg-slate-900 border-white/5 text-slate-400'
              }`}
            >
              Chuyển khoản (VietQR)
            </button>
            <button
              onClick={() => setPaymentMethod('Cash')}
              className={`flex-1 py-2 rounded-xl text-xs font-semibold border transition-all ${
                paymentMethod === 'Cash'
                  ? 'bg-indigo-600/10 border-indigo-500 text-indigo-400'
                  : 'bg-slate-900 border-white/5 text-slate-400'
              }`}
            >
              Tiền mặt / Offline
            </button>
          </div>

          {/* Hiển thị VietQR */}
          {paymentMethod === 'VietQR' && (
            <div className="flex flex-col items-center justify-center space-y-4">
              {qrUrl ? (
                <div className="p-4 rounded-2xl bg-white flex items-center justify-center shadow-lg border border-white/10 w-64 h-64 overflow-hidden">
                  <img src={qrUrl} alt="VietQR dynamic" className="w-full h-full object-contain" />
                </div>
              ) : (
                <div className="p-4 rounded-2xl bg-slate-900 border border-white/5 text-center text-xs text-slate-400 flex flex-col items-center py-8">
                  <AlertCircle className="text-amber-400 mb-2" size={24} />
                  <p>Chủ nợ chưa điền thông tin tài khoản thụ hưởng.</p>
                  <p className="mt-1 text-[10px] text-slate-500">Mã QR động sẽ không khả dụng. Vui lòng thanh toán offline.</p>
                </div>
              )}

              {qrUrl && (
                <div className="text-center space-y-1">
                  <p className="text-xs text-indigo-400 font-semibold uppercase">{bankCode} - {bankAccountNo}</p>
                  <p className="text-[10px] text-slate-500 font-semibold">{bankAccountName}</p>
                  {transferReference && (
                    <p className="text-[10px] text-emerald-400 bg-emerald-500/10 px-3 py-1 rounded-md inline-block font-mono">
                      Ref: {transferReference}
                    </p>
                  )}
                  <p className="text-[10px] text-slate-400 bg-white/5 px-3 py-1 rounded-md inline-block font-mono mt-1">
                    ND: {memoText}
                  </p>
                </div>
              )}

              {qrUrl && isMobile && (
                <div className="w-full pt-4 border-t border-white/5 space-y-2">
                  <p className="text-[10px] text-slate-400 text-center font-medium">
                    Mở nhanh bằng App Ngân hàng trên điện thoại:
                  </p>
                  <div className="grid grid-cols-4 gap-2 w-full">
                    {POPULAR_MOBILE_BANKS.map((b) => {
                      const link = `https://dl.vietqr.io/pay?app=${b.id}&stk=${bankAccountNo}&amount=${amount}&nd=${encodeURIComponent(transferReference || memoText)}&accountName=${encodeURIComponent(bankAccountName || '')}`;
                      return (
                        <a
                          key={b.id}
                          href={link}
                          className="flex flex-col items-center justify-center p-2 rounded-xl bg-slate-900 border border-white/5 hover:border-indigo-500 hover:bg-slate-800 transition-all text-center text-[9px] font-bold text-slate-300 active:scale-95"
                        >
                          <span className="text-indigo-400 mb-0.5 text-xs font-extrabold">{b.logo}</span>
                          <span className="truncate w-full">{b.name}</span>
                        </a>
                      );
                    })}
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Tải ảnh chuyển khoản (Minh chứng) */}
          <div className="space-y-2">
            <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider block">Minh chứng chuyển khoản (Không bắt buộc)</label>
            <div className="flex items-center space-x-3">
              <label className="flex items-center space-x-2 py-2 px-3 rounded-lg bg-slate-900 border border-white/5 hover:bg-slate-800 cursor-pointer text-xs text-slate-300 transition-colors">
                <Upload size={14} />
                <span>Upload hóa đơn</span>
                <input
                  type="file"
                  accept="image/*"
                  onChange={handleFileChange}
                  className="hidden"
                />
              </label>

              {uploading && <span className="text-[10px] text-slate-500">Đang tải...</span>}
              {proofUrl && !uploading && (
                <span className="text-xs text-emerald-400 flex items-center space-x-1">
                  <Check size={14} />
                  <span>Đã lưu ảnh</span>
                </span>
              )}
            </div>
            {proofUrl && (
              <div className="mt-2 w-20 h-20 rounded-lg overflow-hidden border border-white/5">
                <img src={proofPreviewUrl || proofUrl} alt="Receipt proof" className="w-full h-full object-cover" />
              </div>
            )}
          </div>

          {/* Hành động */}
          <div className="flex space-x-3 pt-2">
            <button
              onClick={onClose}
              className="flex-1 py-3 rounded-xl bg-white/5 hover:bg-white/10 text-xs font-bold text-slate-300 transition-colors"
            >
              {translate('common.cancel')}
            </button>
            <button
              onClick={handleSettleSubmit}
              disabled={submitting || uploading}
              className="flex-1 py-3 rounded-xl bg-gradient-to-r from-emerald-600 to-emerald-500 hover:from-emerald-500 hover:to-emerald-600 text-xs font-bold shadow-lg shadow-emerald-500/20 transition-all disabled:opacity-50"
            >
              {submitting ? translate('common.loading') : translate('group.confirm_sent')}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default SettleModal;
