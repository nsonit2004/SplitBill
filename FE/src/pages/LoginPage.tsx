import React, { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useTranslation } from 'react-i18next';
import api from '../services/api';
import { 
  Mail, 
  Lock, 
  User, 
  Wallet, 
  ChevronDown, 
  ChevronUp, 
  ArrowRight,
  Globe
} from 'lucide-react';

const BANK_OPTIONS = [
  { code: 'VCB', label: 'Vietcombank (VCB)' },
  { code: 'TCB', label: 'Techcombank (TCB)' },
  { code: 'MB', label: 'MB Bank (MB)' },
  { code: 'BIDV', label: 'BIDV' },
  { code: 'CTG', label: 'VietinBank (CTG)' },
  { code: 'ACB', label: 'ACB' },
  { code: 'VPB', label: 'VPBank (VPB)' },
  { code: 'TPB', label: 'TPBank (TPB)' },
  { code: 'STB', label: 'Sacombank (STB)' },
  { code: 'VIB', label: 'VIB' }
];

const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useAuth();
  const { t, i18n } = useTranslation();
  
  const [isLogin, setIsLogin] = useState(true);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [showBankInfo, setShowBankInfo] = useState(false);

  // Form Fields
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [bankCode, setBankCode] = useState('');
  const [bankAccountNo, setBankAccountNo] = useState('');
  const [bankAccountName, setBankAccountName] = useState('');
  const inviteToken = new URLSearchParams(location.search).get('inviteToken');

  const toggleLanguage = () => {
    const nextLang = i18n.language === 'vi' ? 'en' : 'vi';
    i18n.changeLanguage(nextLang);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      if (isLogin) {
        // Đăng nhập
        const response = await api.post('/auth/login', { email, password });
        login(response.data.token, {
          userId: response.data.userId,
          email: response.data.email,
          displayName: response.data.displayName,
          avatarUrl: response.data.avatarUrl,
          bankCode: response.data.bankCode,
          bankAccountNo: response.data.bankAccountNo,
          bankAccountName: response.data.bankAccountName
        });
        navigate(inviteToken ? `/join/${inviteToken}` : '/dashboard');
      } else {
        // Đăng ký
        const response = await api.post('/auth/register', {
          email,
          password,
          displayName,
          bankCode: bankCode || null,
          bankAccountNo: bankAccountNo || null,
          bankAccountName: bankAccountName || null
        });
        login(response.data.token, {
          userId: response.data.userId,
          email: response.data.email,
          displayName: response.data.displayName,
          avatarUrl: response.data.avatarUrl,
          bankCode: response.data.bankCode,
          bankAccountNo: response.data.bankAccountNo,
          bankAccountName: response.data.bankAccountName
        });
        navigate(inviteToken ? `/join/${inviteToken}` : '/dashboard');
      }
    } catch (err: any) {
      const msg = err.response?.data?.message || t('common.error');
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#0b0f19] text-[#f8fafc] flex flex-col justify-center items-center p-4 font-sans relative">
      {/* Hiệu ứng nền mờ */}
      <div className="absolute w-[400px] h-[400px] rounded-full bg-indigo-500/10 blur-[120px] pointer-events-none top-[10%]"></div>

      {/* Button chuyển ngôn ngữ góc trên */}
      <div className="absolute top-4 right-4 z-10">
        <button 
          onClick={toggleLanguage}
          className="flex items-center space-x-1 py-1.5 px-3 rounded-lg bg-white/5 hover:bg-white/10 text-xs transition-colors border border-white/5"
        >
          <Globe size={14} />
          <span>{i18n.language.toUpperCase()}</span>
        </button>
      </div>

      <div className="w-full max-w-md bg-[#0f172a]/60 backdrop-blur-xl border border-white/5 p-8 rounded-3xl shadow-2xl relative">
        <div className="flex flex-col items-center mb-8">
          <div 
            onClick={() => navigate('/')} 
            className="w-12 h-12 rounded-2xl bg-gradient-to-tr from-[#6366f1] to-[#10b981] flex items-center justify-center font-bold text-2xl shadow-lg shadow-indigo-500/20 cursor-pointer"
          >
            SB
          </div>
          <h2 className="mt-4 text-2xl font-extrabold tracking-tight bg-gradient-to-r from-white to-slate-300 bg-clip-text text-transparent">
            {isLogin ? t('common.login') : t('common.register')}
          </h2>
          <p className="text-sm text-slate-400 mt-1">
            {isLogin ? 'Đăng nhập vào SplitBill Pro' : 'Tạo tài khoản mới'}
          </p>
        </div>

        {error && (
          <div className="mb-6 p-4 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-400 text-sm">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          {!isLogin && (
            <div className="relative">
              <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-500">
                <User size={18} />
              </span>
              <input
                type="text"
                required
                placeholder={t('auth.display_name')}
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                className="w-full pl-10 pr-4 py-3 rounded-xl bg-slate-900/60 border border-white/5 focus:border-indigo-500 focus:outline-none text-sm transition-colors text-white"
              />
            </div>
          )}

          <div className="relative">
            <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-500">
              <Mail size={18} />
            </span>
            <input
              type="email"
              required
              placeholder={t('auth.email')}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full pl-10 pr-4 py-3 rounded-xl bg-slate-900/60 border border-white/5 focus:border-indigo-500 focus:outline-none text-sm transition-colors text-white"
            />
          </div>

          <div className="relative">
            <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-500">
              <Lock size={18} />
            </span>
            <input
              type="password"
              required
              placeholder={t('auth.password')}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full pl-10 pr-4 py-3 rounded-xl bg-slate-900/60 border border-white/5 focus:border-indigo-500 focus:outline-none text-sm transition-colors text-white"
            />
          </div>

          {/* Accordion điền thông tin Ngân hàng (chỉ hiển thị khi đăng ký) */}
          {!isLogin && (
            <div className="border border-white/5 rounded-xl overflow-hidden bg-slate-900/30">
              <button
                type="button"
                onClick={() => setShowBankInfo(!showBankInfo)}
                className="w-full px-4 py-3 flex items-center justify-between text-xs font-semibold text-indigo-400 hover:bg-white/5 transition-colors"
              >
                <span className="flex items-center space-x-2">
                  <Wallet size={14} />
                  <span>{t('auth.bank_info_title')}</span>
                </span>
                {showBankInfo ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
              </button>

              {showBankInfo && (
                <div className="p-4 border-t border-white/5 space-y-3 bg-slate-950/20">
                  <div>
                    <select
                      value={bankCode}
                      onChange={(e) => setBankCode(e.target.value)}
                      className="w-full px-3 py-2.5 rounded-lg bg-slate-900/80 border border-white/5 focus:border-indigo-500 focus:outline-none text-xs text-white"
                    >
                      <option value="">{t('auth.bank_code_select')}</option>
                      {BANK_OPTIONS.map((bank) => (
                        <option key={bank.code} value={bank.code}>
                          {bank.label}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <input
                      type="text"
                      placeholder={t('auth.bank_account_no')}
                      value={bankAccountNo}
                      onChange={(e) => setBankAccountNo(e.target.value)}
                      className="w-full px-3 py-2.5 rounded-lg bg-slate-900/80 border border-white/5 focus:border-indigo-500 focus:outline-none text-xs text-white"
                    />
                  </div>
                  <div>
                    <input
                      type="text"
                      placeholder={t('auth.bank_account_name')}
                      value={bankAccountName}
                      onChange={(e) => setBankAccountName(e.target.value)}
                      className="w-full px-3 py-2.5 rounded-lg bg-slate-900/80 border border-white/5 focus:border-indigo-500 focus:outline-none text-xs text-white"
                    />
                  </div>
                </div>
              )}
            </div>
          )}

          <button
            type="submit"
            disabled={loading}
            className="w-full py-3 rounded-xl bg-gradient-to-r from-indigo-600 to-indigo-500 hover:from-indigo-500 hover:to-indigo-600 text-sm font-bold shadow-lg shadow-indigo-500/20 transition-all flex items-center justify-center space-x-2 active:scale-95 disabled:opacity-50"
          >
            <span>{loading ? t('common.loading') : isLogin ? t('common.login') : t('common.register')}</span>
            {!loading && <ArrowRight size={16} />}
          </button>
        </form>

        <div className="mt-6 text-center text-sm text-slate-400">
          {isLogin ? (
            <p>
              {t('auth.no_account')}{' '}
              <button 
                onClick={() => setIsLogin(false)} 
                className="text-indigo-400 hover:underline font-semibold"
              >
                {t('common.register')}
              </button>
            </p>
          ) : (
            <p>
              {t('auth.have_account')}{' '}
              <button 
                onClick={() => setIsLogin(true)} 
                className="text-indigo-400 hover:underline font-semibold"
              >
                {t('common.login')}
              </button>
            </p>
          )}
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
