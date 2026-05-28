import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import api from '../services/api';
import { X, Upload, Check, Sparkles, RefreshCw } from 'lucide-react';

interface Member {
  id: string;
  nickname: string;
}

interface ExpenseModalProps {
  isOpen: boolean;
  onClose: () => void;
  groupId: string;
  members: Member[];
  onSuccess: () => void;
}

const ExpenseModal: React.FC<ExpenseModalProps> = ({
  isOpen,
  onClose,
  groupId,
  members,
  onSuccess
}) => {
  const { t } = useTranslation();
  const [description, setDescription] = useState('');
  const [totalAmount, setTotalAmount] = useState<number>(0);
  const [splitMethod, setSplitMethod] = useState<'Equally' | 'Amount' | 'Shares' | 'Exclude'>('Equally');
  
  // Payers: MemberId -> Số tiền trả
  const [payerAmounts, setPayerAmounts] = useState<Record<string, number>>({});
  // Slices: MemberId -> Giá trị chia (số tiền, số phần, hoặc 1/0)
  const [sliceValues, setSliceValues] = useState<Record<string, number>>({});
  
  // Image Upload State
  const [imageUrl, setImageUrl] = useState('');
  const [uploadingImage, setUploadingImage] = useState(false);
  const [submitError, setSubmitError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  // AI OCR Scan State
  const [isOcrMode, setIsOcrMode] = useState(false);
  const [scanningOcr, setScanningOcr] = useState(false);
  const [ocrResult, setOcrResult] = useState<{
    merchantName: string;
    date?: string;
    tax: number;
    serviceCharge: number;
    totalAmount: number;
    items: Array<{ name: string; quantity: number; unitPrice: number; totalPrice: number }>;
  } | null>(null);
  const [ocrMerchantName, setOcrMerchantName] = useState('');
  const [ocrDate, setOcrDate] = useState('');
  const [ocrTax, setOcrTax] = useState(0);
  const [ocrServiceCharge, setOcrServiceCharge] = useState(0);
  const [ocrPayer, setOcrPayer] = useState('');
  const [feeSplitMethod, setFeeSplitMethod] = useState<'Equally' | 'Proportionally'>('Equally');
  const [selectedItemConsumers, setSelectedItemConsumers] = useState<Record<number, string[]>>({});

  // Khởi tạo dữ liệu khi mở modal, nhưng không reset dữ liệu đã nhập
  // khi danh sách members được refresh định kỳ từ realtime polling.
  useEffect(() => {
    if (isOpen && members.length > 0) {
      setPayerAmounts((prev) => {
        const next: Record<string, number> = {};
        members.forEach((m) => {
          next[m.id] = prev[m.id] ?? 0;
        });
        return next;
      });

      setSliceValues((prev) => {
        const next: Record<string, number> = {};
        members.forEach((m) => {
          const defaultSlice = splitMethod === 'Shares' ? 1 : splitMethod === 'Exclude' ? 1 : 0;
          next[m.id] = prev[m.id] ?? defaultSlice;
        });
        return next;
      });
    }
  }, [isOpen, members, splitMethod]);

  const toggleItemConsumer = (itemIdx: number, memberId: string) => {
    setSelectedItemConsumers(prev => {
      const current = prev[itemIdx] || [];
      const updated = current.includes(memberId)
        ? current.filter(id => id !== memberId)
        : [...current, memberId];
      return {
        ...prev,
        [itemIdx]: updated
      };
    });
  };

  const calculateMemberShares = () => {
    if (!ocrResult) return {};

    const shares: Record<string, { subtotal: number; fees: number; total: number }> = {};
    members.forEach(m => {
      shares[m.id] = { subtotal: 0, fees: 0, total: 0 };
    });

    let totalItemsPrice = 0;
    ocrResult.items.forEach((item, idx) => {
      totalItemsPrice += item.totalPrice;
      const consumers = selectedItemConsumers[idx] || [];
      if (consumers.length > 0) {
        const itemShare = item.totalPrice / consumers.length;
        consumers.forEach(memberId => {
          if (shares[memberId]) {
            shares[memberId].subtotal += itemShare;
          }
        });
      }
    });

    const totalFees = ocrTax + ocrServiceCharge;
    if (totalFees > 0) {
      if (feeSplitMethod === 'Equally') {
        const feeShare = totalFees / members.length;
        members.forEach(m => {
          shares[m.id].fees = feeShare;
        });
      } else {
        members.forEach(m => {
          const ratio = totalItemsPrice > 0 ? shares[m.id].subtotal / totalItemsPrice : 1 / members.length;
          shares[m.id].fees = totalFees * ratio;
        });
      }
    }

    members.forEach(m => {
      shares[m.id].total = shares[m.id].subtotal + shares[m.id].fees;
    });

    return shares;
  };

  const applyOcrSplits = () => {
    if (!ocrResult) return;

    const shares = calculateMemberShares();
    setDescription(`${ocrMerchantName} (${ocrDate})`);
    
    const calculatedTotal = Object.values(shares).reduce((sum, s) => sum + s.total, 0);
    setTotalAmount(Math.round(calculatedTotal));
    setSplitMethod('Amount');

    const nextSliceValues: Record<string, number> = {};
    members.forEach(m => {
      nextSliceValues[m.id] = Math.round(shares[m.id]?.total || 0);
    });
    setSliceValues(nextSliceValues);

    const nextPayerAmounts: Record<string, number> = {};
    members.forEach(m => {
      nextPayerAmounts[m.id] = m.id === ocrPayer ? Math.round(calculatedTotal) : 0;
    });
    setPayerAmounts(nextPayerAmounts);

    setIsOcrMode(false);
  };

  const handleOcrFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setScanningOcr(true);
    setSubmitError('');

    const formData = new FormData();
    formData.append('file', file);

    try {
      const responseImg = await api.post('/expenses/upload', formData, {
        headers: {
          'Content-Type': 'multipart/form-data'
        }
      });
      const url = responseImg.data?.imageUrl;
      if (url) {
        setImageUrl(url);
      }
    } catch (err) {
      console.error("Lỗi upload ảnh lên Cloudinary:", err);
    }

    try {
      const responseOcr = await api.post('/expenses/scan-receipt', formData, {
        headers: {
          'Content-Type': 'multipart/form-data'
        }
      });
      const data = responseOcr.data;
      if (!data || !data.items) {
        setSubmitError('Không thể trích xuất các món ăn từ hóa đơn này. Vui lòng thử lại hoặc nhập thủ công.');
        return;
      }
      
      setOcrResult(data);
      setOcrMerchantName(data.merchantName || 'Hóa đơn quét');
      
      const dateStr = data.date ? data.date.substring(0, 10) : new Date().toISOString().substring(0, 10);
      setOcrDate(dateStr);
      setOcrTax(data.tax || 0);
      setOcrServiceCharge(data.serviceCharge || 0);
      setOcrPayer(members[0]?.id || '');
      setFeeSplitMethod('Equally');
      
      const initialConsumers: Record<number, string[]> = {};
      const allMemberIds = members.map(m => m.id);
      data.items.forEach((_: any, idx: number) => {
        initialConsumers[idx] = [...allMemberIds];
      });
      setSelectedItemConsumers(initialConsumers);
      setIsOcrMode(true);
    } catch (err: any) {
      setSubmitError(err.response?.data?.message || 'Không thể quét hóa đơn. Vui lòng kiểm tra lại ảnh hoặc cấu hình API Key.');
    } finally {
      setScanningOcr(false);
      e.target.value = '';
    }
  };

  if (!isOpen) return null;

  if (isOcrMode && ocrResult) {
    const shares = calculateMemberShares();
    const calculatedTotal = Object.values(shares).reduce((sum, s) => sum + s.total, 0);

    return (
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4 overflow-y-auto">
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm" onClick={() => setIsOcrMode(false)}></div>

        <div className="w-full max-w-3xl bg-[#0f172a] border border-white/10 rounded-3xl p-6 md:p-8 shadow-2xl relative z-10 max-h-[90vh] overflow-y-auto animate-in fade-in zoom-in-95 duration-200">
          <button
            onClick={() => setIsOcrMode(false)}
            className="absolute top-4 right-4 p-2 text-slate-400 hover:text-slate-200 rounded-lg hover:bg-white/5 transition-all"
          >
            <X size={20} />
          </button>

          <h2 className="text-2xl font-extrabold text-white mb-2 flex items-center gap-2">
            <Sparkles className="text-indigo-400" size={24} />
            Chia tiền theo món ăn (AI OCR)
          </h2>
          <p className="text-xs text-slate-400 mb-6">Hãy chọn các thành viên đã ăn/sử dụng từng món dưới đây để tính tiền chính xác.</p>

          <div className="space-y-6">
            {/* Tên hóa đơn & Người trả */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Tên cửa hàng/Mô tả</label>
                <input
                  type="text"
                  value={ocrMerchantName}
                  onChange={(e) => setOcrMerchantName(e.target.value)}
                  className="w-full px-4 py-2.5 rounded-xl bg-slate-900 border border-white/5 focus:border-indigo-500 focus:outline-none text-sm text-white font-medium"
                />
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Người thanh toán</label>
                <select
                  value={ocrPayer}
                  onChange={(e) => setOcrPayer(e.target.value)}
                  className="w-full px-4 py-2.5 rounded-xl bg-slate-900 border border-white/5 focus:border-indigo-500 focus:outline-none text-sm text-white"
                >
                  {members.map(m => (
                    <option key={m.id} value={m.id}>{m.nickname}</option>
                  ))}
                </select>
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Ngày hóa đơn</label>
                <input
                  type="date"
                  value={ocrDate}
                  onChange={(e) => setOcrDate(e.target.value)}
                  className="w-full px-4 py-2.5 rounded-xl bg-slate-900 border border-white/5 focus:border-indigo-500 focus:outline-none text-sm text-white"
                />
              </div>
            </div>

            {/* Phân chia thuế & phí */}
            <div className="p-4 rounded-2xl bg-slate-900/30 border border-white/5 space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Tiền thuế VAT (đ)</label>
                  <input
                    type="number"
                    value={ocrTax || ''}
                    onChange={(e) => setOcrTax(parseFloat(e.target.value) || 0)}
                    className="w-full px-4 py-2 rounded-xl bg-slate-900 border border-white/5 focus:border-indigo-500 focus:outline-none text-xs text-white"
                  />
                </div>
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Phí dịch vụ / Khác (đ)</label>
                  <input
                    type="number"
                    value={ocrServiceCharge || ''}
                    onChange={(e) => setOcrServiceCharge(parseFloat(e.target.value) || 0)}
                    className="w-full px-4 py-2 rounded-xl bg-slate-900 border border-white/5 focus:border-indigo-500 focus:outline-none text-xs text-white"
                  />
                </div>
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider block">Cách phân chia thuế & phí</label>
                <div className="flex space-x-6">
                  <label className="flex items-center space-x-2 text-xs text-slate-300 cursor-pointer">
                    <input
                      type="radio"
                      name="feeSplit"
                      checked={feeSplitMethod === 'Equally'}
                      onChange={() => setFeeSplitMethod('Equally')}
                      className="text-indigo-600 focus:ring-indigo-500 bg-slate-900 border-white/5"
                    />
                    <span>Chia đều</span>
                  </label>
                  <label className="flex items-center space-x-2 text-xs text-slate-300 cursor-pointer">
                    <input
                      type="radio"
                      name="feeSplit"
                      checked={feeSplitMethod === 'Proportionally'}
                      onChange={() => setFeeSplitMethod('Proportionally')}
                      className="text-indigo-600 focus:ring-indigo-500 bg-slate-900 border-white/5"
                    />
                    <span>Chia tỷ lệ theo món</span>
                  </label>
                </div>
              </div>
            </div>

            {/* Danh sách món ăn và chọn người ăn */}
            <div className="space-y-3">
              <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider block">
                Danh sách món ăn từ hóa đơn ({ocrResult.items.length} món)
              </label>
              <div className="border border-white/5 rounded-2xl overflow-hidden bg-slate-900/20 divide-y divide-white/5 max-h-[35vh] overflow-y-auto pr-2">
                {ocrResult.items.map((item, idx) => {
                  const consumers = selectedItemConsumers[idx] || [];
                  const costPerPerson = consumers.length > 0 ? item.totalPrice / consumers.length : 0;

                  return (
                    <div key={idx} className="p-4 space-y-3">
                      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                        <div>
                          <span className="text-sm font-bold text-white">{item.name}</span>
                          <span className="text-xs text-slate-400 ml-2">
                            (SL: {item.quantity} x {item.unitPrice.toLocaleString()}đ)
                          </span>
                        </div>
                        <span className="text-sm font-extrabold text-indigo-400">{item.totalPrice.toLocaleString()}đ</span>
                      </div>

                      {/* Nút chọn thành viên ăn món này */}
                      <div className="flex flex-wrap gap-2 pt-1">
                        {members.map(m => {
                          const isSelected = consumers.includes(m.id);
                          return (
                            <button
                              key={m.id}
                              type="button"
                              onClick={() => toggleItemConsumer(idx, m.id)}
                              className={`px-3 py-1.5 rounded-full text-xs font-medium border flex items-center space-x-1.5 transition-all ${
                                isSelected
                                  ? 'bg-indigo-600/20 border-indigo-500 text-indigo-300'
                                  : 'bg-slate-900 border-white/5 text-slate-400 hover:text-slate-200'
                              }`}
                            >
                              <span className={`w-1.5 h-1.5 rounded-full ${isSelected ? 'bg-indigo-400 animate-pulse' : 'bg-slate-600'}`}></span>
                              <span>{m.nickname}</span>
                              {isSelected && (
                                <span className="text-[10px] text-indigo-400 font-bold ml-1">
                                  (+{costPerPerson.toLocaleString(undefined, {maximumFractionDigits: 0})}đ)
                                </span>
                              )}
                            </button>
                          );
                        })}
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>

            {/* Bảng tổng hợp số tiền mỗi người */}
            <div className="p-5 rounded-2xl bg-slate-950 border border-white/10 space-y-4">
              <h4 className="text-sm font-bold text-white border-b border-white/5 pb-2">Bảng tổng hợp chia tiền chi tiết</h4>
              
              <div className="space-y-2 max-h-32 overflow-y-auto pr-1">
                {members.map(m => {
                  const share = shares[m.id] || { subtotal: 0, fees: 0, total: 0 };

                  return (
                    <div key={m.id} className="flex justify-between items-center text-xs text-slate-300">
                      <span>{m.nickname}</span>
                      <div className="flex items-center space-x-4">
                        <span className="text-[10px] text-slate-500">
                          Món: {share.subtotal.toLocaleString(undefined, {maximumFractionDigits:0})}đ
                          {share.fees > 0 && ` + Thuế/phí: ${share.fees.toLocaleString(undefined, {maximumFractionDigits:0})}đ`}
                        </span>
                        <span className="font-bold text-white">{share.total.toLocaleString(undefined, {maximumFractionDigits:0})}đ</span>
                      </div>
                    </div>
                  );
                })}
              </div>

              <div className="border-t border-white/5 pt-3 flex justify-between items-center font-extrabold text-sm text-white">
                <span>Tổng chi phí đã chia</span>
                <div className="flex items-center space-x-3">
                  <span className="text-xs font-normal text-slate-400">
                    Hóa đơn gốc: {ocrResult.totalAmount.toLocaleString()}đ
                  </span>
                  <span className="text-indigo-400 font-bold text-base">
                    {Math.round(calculatedTotal).toLocaleString()}đ
                  </span>
                </div>
              </div>
            </div>

            {/* Nút hành động */}
            <div className="flex space-x-3 pt-2">
              <button
                type="button"
                onClick={() => setIsOcrMode(false)}
                className="flex-1 py-3 rounded-xl bg-white/5 hover:bg-white/10 text-sm font-bold text-slate-300 transition-colors"
              >
                Quay lại
              </button>
              <button
                type="button"
                onClick={applyOcrSplits}
                className="flex-1 py-3 rounded-xl bg-gradient-to-r from-indigo-600 to-indigo-500 hover:from-indigo-500 hover:to-indigo-600 text-sm font-bold shadow-lg shadow-indigo-500/20 transition-all"
              >
                Áp dụng & Điền form
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  const handleImageChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setUploadingImage(true);
    setSubmitError('');

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
        setSubmitError('Upload ảnh thành công nhưng không nhận được URL ảnh. Vui lòng thử lại.');
        setImageUrl('');
        return;
      }
      setImageUrl(url);
    } catch (err: any) {
      setSubmitError(err.response?.data?.message || 'Không thể tải ảnh lên. Vui lòng thử lại.');
    } finally {
      setUploadingImage(false);
    }
  };

  const handlePayerAmountChange = (memberId: string, val: number) => {
    setPayerAmounts(prev => ({
      ...prev,
      [memberId]: val
    }));
  };

  const handleSliceValueChange = (memberId: string, val: number) => {
    setSliceValues(prev => ({
      ...prev,
      [memberId]: val
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitError('');

    // Kiểm tra tổng tiền người trả
    const totalPaid = Object.values(payerAmounts).reduce((a, b) => a + b, 0);
    if (Math.abs(totalPaid - totalAmount) > 1) {
      const diff = totalAmount - totalPaid;
      if (diff > 0) {
        setSubmitError(`Bạn còn thiếu ${diff.toLocaleString()}đ ở mục "Ai trả tiền?". Tổng người trả (${totalPaid.toLocaleString()}đ) phải bằng tổng hóa đơn (${totalAmount.toLocaleString()}đ).`);
      } else {
        setSubmitError(`Bạn đang nhập dư ${Math.abs(diff).toLocaleString()}đ ở mục "Ai trả tiền?". Tổng người trả (${totalPaid.toLocaleString()}đ) phải bằng tổng hóa đơn (${totalAmount.toLocaleString()}đ).`);
      }
      return;
    }

    // Kiểm tra logic theo split method
    if (splitMethod === 'Amount') {
      const totalSliceSum = Object.values(sliceValues).reduce((a, b) => a + b, 0);
      if (Math.abs(totalSliceSum - totalAmount) > 1) {
        setSubmitError(`Tổng số tiền chia cho các thành viên (${totalSliceSum.toLocaleString()}đ) phải bằng Tổng hóa đơn (${totalAmount.toLocaleString()}đ).`);
        return;
      }
    }

    setSubmitting(true);

    try {
      // Map payers
      const payersPayload = Object.entries(payerAmounts)
        .filter(([_, val]) => val > 0)
        .map(([id, val]) => ({
          memberId: id,
          amountPaid: val
        }));

      // Map slices
      const slicesPayload = members.map(m => {
        let val = sliceValues[m.id] || 0;
        if (splitMethod === 'Equally') {
          val = 1; // Mặc định tất cả nhận 1 phần bằng nhau
        }
        return {
          memberId: m.id,
          value: val
        };
      });

      await api.post(`/expenses/group/${groupId}`, {
        description,
        totalAmount,
        splitMethod,
        imageUrl: imageUrl || null,
        payers: payersPayload,
        slices: slicesPayload
      });

      onSuccess();
      onClose();
    } catch (err: any) {
      setSubmitError(err.response?.data?.message || 'Có lỗi xảy ra khi tạo hóa đơn.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 overflow-y-auto">
      <div className="fixed inset-0 bg-black/60 backdrop-blur-sm" onClick={onClose}></div>

      <div className="w-full max-w-2xl bg-[#0f172a] border border-white/10 rounded-3xl p-6 md:p-8 shadow-2xl relative z-10 max-h-[90vh] overflow-y-auto animate-in fade-in zoom-in-95 duration-200">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 p-2 text-slate-400 hover:text-slate-200 rounded-lg hover:bg-white/5 transition-all"
        >
          <X size={20} />
        </button>

        <h2 className="text-2xl font-extrabold text-white mb-6">
          {t('group.add_expense')}
        </h2>

        {submitError && (
          <div className="mb-6 p-4 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-400 text-sm">
            {submitError}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Banner AI OCR Quét hóa đơn */}
          <div className="p-4 rounded-2xl bg-gradient-to-r from-indigo-950/50 via-slate-900 to-indigo-950/20 border border-indigo-500/20 flex flex-col sm:flex-row items-center justify-between gap-4">
            <div className="flex items-center space-x-3">
              <div className="p-2 rounded-xl bg-indigo-500/10 text-indigo-400">
                <Sparkles size={20} className="animate-pulse" />
              </div>
              <div className="text-left">
                <h4 className="text-sm font-bold text-white flex items-center gap-1.5">
                  Quét hóa đơn bằng AI
                  <span className="text-[10px] bg-indigo-600/30 text-indigo-300 px-1.5 py-0.5 rounded-full font-semibold">Mới</span>
                </h4>
                <p className="text-xs text-slate-400">Tự động đọc danh sách món ăn và chia đều/theo món trực quan</p>
              </div>
            </div>
            <label className="flex items-center space-x-2 py-2.5 px-4 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-xs font-bold text-white cursor-pointer transition-all shadow-lg shadow-indigo-600/20 active:scale-95 disabled:opacity-50">
              {scanningOcr ? (
                <RefreshCw size={14} className="animate-spin" />
              ) : (
                <Upload size={14} />
              )}
              <span>{scanningOcr ? "Đang quét..." : "Chọn ảnh hóa đơn"}</span>
              <input
                type="file"
                accept="image/*"
                onChange={handleOcrFileChange}
                disabled={scanningOcr}
                className="hidden"
              />
            </label>
          </div>

          {/* Mô tả & Số tiền */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider">{t('common.description')}</label>
              <input
                type="text"
                required
                placeholder="Ví dụ: Tiền ăn trưa lẩu"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                className="w-full px-4 py-3 rounded-xl bg-slate-900 border border-white/5 focus:border-indigo-500 focus:outline-none text-sm text-white"
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider">{t('common.amount')} (đ)</label>
              <input
                type="number"
                required
                min={1000}
                placeholder="Ví dụ: 150000"
                value={totalAmount || ''}
                onChange={(e) => setTotalAmount(parseFloat(e.target.value) || 0)}
                className="w-full px-4 py-3 rounded-xl bg-slate-900 border border-white/5 focus:border-indigo-500 focus:outline-none text-sm text-white font-bold"
              />
            </div>
          </div>

          {/* Phần 1: Ai trả tiền? (Payers) */}
          <div className="space-y-3">
            <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider block">
              {t('group.who_paid')}
            </label>
            <div className="max-h-36 overflow-y-auto space-y-2 pr-2 border border-white/5 rounded-xl p-3 bg-slate-900/30">
              {members.map(m => (
                <div key={m.id} className="flex items-center justify-between space-x-4">
                  <span className="text-sm font-medium text-slate-300 truncate">{m.nickname}</span>
                  <div className="flex items-center space-x-2">
                    <input
                      type="number"
                      min={0}
                      placeholder="0"
                      value={payerAmounts[m.id] || ''}
                      onChange={(e) => handlePayerAmountChange(m.id, parseFloat(e.target.value) || 0)}
                      className="w-32 px-3 py-1.5 rounded-lg bg-slate-900 border border-white/5 focus:border-indigo-500 focus:outline-none text-xs text-white text-right"
                    />
                    <span className="text-[10px] text-slate-500">đ</span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Cách chia (Split Method Selector) */}
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider">{t('group.split_method')}</label>
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
              {(['Equally', 'Amount', 'Shares', 'Exclude'] as const).map((method) => (
                <button
                  key={method}
                  type="button"
                  onClick={() => setSplitMethod(method)}
                  className={`py-2 px-3 rounded-xl border text-xs font-semibold transition-all ${
                    splitMethod === method
                      ? 'bg-indigo-600/10 border-indigo-500 text-indigo-400'
                      : 'bg-slate-900 border-white/5 text-slate-400 hover:text-slate-200'
                  }`}
                >
                  {t(`group.split_${method.toLowerCase()}`)}
                </button>
              ))}
            </div>
          </div>

          {/* Phần 2: Điền thông tin chia (Slices Inputs) */}
          {splitMethod !== 'Equally' && (
            <div className="space-y-3">
              <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider block">
                Chi tiết chia tiền ({t(`group.split_${splitMethod.toLowerCase()}`)})
              </label>
              <div className="max-h-40 overflow-y-auto space-y-2 pr-2 border border-white/5 rounded-xl p-3 bg-slate-900/30">
                {members.map(m => (
                  <div key={m.id} className="flex items-center justify-between space-x-4">
                    <span className="text-sm font-medium text-slate-300 truncate">{m.nickname}</span>
                    
                    {splitMethod === 'Amount' && (
                      <div className="flex items-center space-x-2">
                        <input
                          type="number"
                          min={0}
                          placeholder="0"
                          value={sliceValues[m.id] || ''}
                          onChange={(e) => handleSliceValueChange(m.id, parseFloat(e.target.value) || 0)}
                          className="w-32 px-3 py-1.5 rounded-lg bg-slate-900 border border-white/5 focus:border-indigo-500 focus:outline-none text-xs text-white text-right"
                        />
                        <span className="text-[10px] text-slate-500">đ</span>
                      </div>
                    )}

                    {splitMethod === 'Shares' && (
                      <div className="flex items-center space-x-2">
                        <input
                          type="number"
                          min={0}
                          placeholder="1"
                          value={sliceValues[m.id] !== undefined ? sliceValues[m.id] : 1}
                          onChange={(e) => handleSliceValueChange(m.id, parseInt(e.target.value) || 0)}
                          className="w-20 px-3 py-1.5 rounded-lg bg-slate-900 border border-white/5 focus:border-indigo-500 focus:outline-none text-xs text-white text-center"
                        />
                        <span className="text-[10px] text-slate-500">phần</span>
                      </div>
                    )}

                    {splitMethod === 'Exclude' && (
                      <input
                        type="checkbox"
                        checked={sliceValues[m.id] !== 0}
                        onChange={(e) => handleSliceValueChange(m.id, e.target.checked ? 1 : 0)}
                        className="w-5 h-5 rounded border-white/5 text-indigo-600 focus:ring-indigo-500 bg-slate-900"
                      />
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Upload ảnh hóa đơn */}
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider block">Ảnh hóa đơn (Không bắt buộc)</label>
            <div className="flex items-center space-x-4">
              <label className="flex items-center space-x-2 py-3 px-4 rounded-xl bg-slate-900 border border-white/5 hover:bg-slate-800 cursor-pointer text-xs text-slate-300 transition-colors">
                <Upload size={16} />
                <span>Chọn ảnh</span>
                <input
                  type="file"
                  accept="image/*"
                  onChange={handleImageChange}
                  className="hidden"
                />
              </label>

              {uploadingImage && (
                <span className="text-xs text-slate-400">Đang tải ảnh lên...</span>
              )}

              {imageUrl && !uploadingImage && (
                <span className="text-xs text-emerald-400 flex items-center space-x-1">
                  <Check size={14} />
                  <span>Đã nhận ảnh hóa đơn</span>
                </span>
              )}
            </div>
            {imageUrl && (
              <div className="mt-2 w-24 h-24 rounded-lg overflow-hidden border border-white/5 relative group">
                <img src={imageUrl} alt="Bill preview" className="w-full h-full object-cover" />
                <button
                  type="button"
                  onClick={() => setImageUrl('')}
                  className="absolute inset-0 bg-black/60 opacity-0 group-hover:opacity-100 flex items-center justify-center text-xs text-rose-400 font-bold transition-opacity"
                >
                  Xóa
                </button>
              </div>
            )}
          </div>

          {/* Submit Buttons */}
          <div className="flex space-x-3 pt-4">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 py-3 rounded-xl bg-white/5 hover:bg-white/10 text-sm font-bold text-slate-300 transition-colors"
            >
              {t('common.cancel')}
            </button>
            <button
              type="submit"
              disabled={submitting || uploadingImage || !description || totalAmount <= 0}
              className="flex-1 py-3 rounded-xl bg-gradient-to-r from-indigo-600 to-indigo-500 hover:from-indigo-500 hover:to-indigo-600 text-sm font-bold shadow-lg shadow-indigo-500/20 transition-all disabled:opacity-50"
            >
              {submitting ? t('common.loading') : t('common.save')}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ExpenseModal;
