import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext';
import { 
  ArrowRight, 
  Coins, 
  QrCode, 
  Users, 
  Globe
} from 'lucide-react';

const LandingPage: React.FC = () => {
  const navigate = useNavigate();
  const { t, i18n } = useTranslation();
  const { user } = useAuth();

  const handleCTA = () => {
    if (user) {
      navigate('/dashboard');
    } else {
      navigate('/login');
    }
  };

  const toggleLanguage = () => {
    const nextLang = i18n.language === 'vi' ? 'en' : 'vi';
    i18n.changeLanguage(nextLang);
  };

  return (
    <div className="min-h-screen bg-[#0b0f19] text-[#f8fafc] flex flex-col font-sans overflow-x-hidden relative">
      {/* Hiệu ứng nền mờ phát sáng */}
      <div className="absolute top-[-20%] left-[-10%] w-[500px] h-[500px] rounded-full bg-indigo-500/10 blur-[150px] pointer-events-none"></div>
      <div className="absolute bottom-[-10%] right-[-10%] w-[600px] h-[600px] rounded-full bg-emerald-500/5 blur-[180px] pointer-events-none"></div>

      {/* Header / Navbar */}
      <header className="w-full max-w-7xl mx-auto px-6 py-6 flex items-center justify-between z-10">
        <div className="flex items-center space-x-3">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-[#6366f1] to-[#10b981] flex items-center justify-center font-bold text-xl shadow-lg shadow-indigo-500/20">
            SB
          </div>
          <span className="font-extrabold text-xl tracking-tight bg-gradient-to-r from-white to-slate-400 bg-clip-text text-transparent">
            SplitBill Pro
          </span>
        </div>

        <div className="flex items-center space-x-4">
          <button 
            onClick={toggleLanguage}
            className="flex items-center space-x-1.5 py-1.5 px-3 rounded-lg bg-white/5 hover:bg-white/10 text-xs font-medium border border-white/5 transition-all"
          >
            <Globe size={14} />
            <span>{i18n.language.toUpperCase()}</span>
          </button>
          <button 
            onClick={handleCTA}
            className="py-2 px-5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-sm font-semibold shadow-lg shadow-indigo-500/20 transition-all active:scale-95"
          >
            {user ? t('dashboard.title') : t('common.login')}
          </button>
        </div>
      </header>

      {/* Hero Section */}
      <main className="flex-1 max-w-7xl mx-auto px-6 w-full flex flex-col justify-center items-center py-16 md:py-24 text-center z-10">
        <div className="inline-flex items-center space-x-2 px-4 py-1.5 rounded-full bg-indigo-500/10 border border-indigo-500/20 text-indigo-400 text-xs font-medium mb-6 animate-pulse">
          <span>✨ New: Tích hợp VietQR Napas 24/7 tự động</span>
        </div>
        
        <h1 className="text-4xl md:text-7xl font-extrabold tracking-tight max-w-4xl leading-tight md:leading-none">
          {t('landing.title')}{' '}
          <span className="bg-gradient-to-r from-indigo-400 via-purple-400 to-emerald-400 bg-clip-text text-transparent">
            Tối Ưu Hóa Nợ
          </span>
        </h1>
        
        <p className="mt-6 text-slate-400 text-lg md:text-xl max-w-2xl leading-relaxed">
          {t('landing.subtitle')}
        </p>

        <div className="mt-10 flex flex-col sm:flex-row space-y-4 sm:space-y-0 sm:space-x-4 w-full sm:w-auto">
          <button
            onClick={handleCTA}
            className="group py-4 px-8 rounded-2xl bg-gradient-to-r from-indigo-600 to-[#4f46e5] hover:from-indigo-500 hover:to-indigo-600 text-base font-bold shadow-lg shadow-indigo-500/30 flex items-center justify-center space-x-2 transition-all active:scale-98"
          >
            <span>{t('landing.get_started')}</span>
            <ArrowRight size={18} className="group-hover:translate-x-1 transition-transform" />
          </button>
        </div>

        {/* Bento Grid Features */}
        <div className="mt-24 w-full grid grid-cols-1 md:grid-cols-3 gap-6 text-left">
          {/* Card 1: Debt Simplification */}
          <div className="p-8 rounded-3xl bg-slate-900/50 backdrop-blur-xl border border-white/5 shadow-2xl relative overflow-hidden flex flex-col justify-between h-72 hover:border-indigo-500/30 transition-all duration-300 group">
            <div className="absolute top-0 right-0 w-24 h-24 bg-indigo-500/10 rounded-full blur-2xl group-hover:bg-indigo-500/20 transition-all"></div>
            <div className="w-12 h-12 rounded-xl bg-indigo-500/10 border border-indigo-500/20 flex items-center justify-center text-indigo-400">
              <Coins size={24} />
            </div>
            <div>
              <h3 className="text-xl font-bold text-white mb-2">{t('landing.feature_1_title')}</h3>
              <p className="text-slate-400 text-sm leading-relaxed">{t('landing.feature_1_desc')}</p>
            </div>
          </div>

          {/* Card 2: VietQR */}
          <div className="p-8 rounded-3xl bg-slate-900/50 backdrop-blur-xl border border-white/5 shadow-2xl relative overflow-hidden flex flex-col justify-between h-72 hover:border-emerald-500/30 transition-all duration-300 group">
            <div className="absolute top-0 right-0 w-24 h-24 bg-emerald-500/5 rounded-full blur-2xl group-hover:bg-emerald-500/10 transition-all"></div>
            <div className="w-12 h-12 rounded-xl bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center text-emerald-400">
              <QrCode size={24} />
            </div>
            <div>
              <h3 className="text-xl font-bold text-white mb-2">{t('landing.feature_2_title')}</h3>
              <p className="text-slate-400 text-sm leading-relaxed">{t('landing.feature_2_desc')}</p>
            </div>
          </div>

          {/* Card 3: Split Methods */}
          <div className="p-8 rounded-3xl bg-slate-900/50 backdrop-blur-xl border border-white/5 shadow-2xl relative overflow-hidden flex flex-col justify-between h-72 hover:border-purple-500/30 transition-all duration-300 group">
            <div className="absolute top-0 right-0 w-24 h-24 bg-purple-500/10 rounded-full blur-2xl group-hover:bg-purple-500/20 transition-all"></div>
            <div className="w-12 h-12 rounded-xl bg-purple-500/10 border border-purple-500/20 flex items-center justify-center text-purple-400">
              <Users size={24} />
            </div>
            <div>
              <h3 className="text-xl font-bold text-white mb-2">{t('landing.feature_3_title')}</h3>
              <p className="text-slate-400 text-sm leading-relaxed">{t('landing.feature_3_desc')}</p>
            </div>
          </div>
        </div>
      </main>

      {/* Footer */}
      <footer className="w-full max-w-7xl mx-auto px-6 py-8 border-t border-white/5 flex flex-col sm:flex-row items-center justify-between text-slate-500 text-sm z-10">
        <p>© 2026 SplitBill Pro. Built for modern group activities.</p>
        <div className="flex space-x-6 mt-4 sm:mt-0">
          <a href="#" className="hover:text-slate-300 transition-colors">Privacy Policy</a>
          <a href="#" className="hover:text-slate-300 transition-colors">Terms of Service</a>
        </div>
      </footer>
    </div>
  );
};

export default LandingPage;
