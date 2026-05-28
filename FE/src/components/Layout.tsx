import React, { useState } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useTranslation } from 'react-i18next';
import { 
  LogOut, 
  Menu, 
  X, 
  Globe, 
  LayoutDashboard
} from 'lucide-react';

interface LayoutProps {
  children: React.ReactNode;
}

const Layout: React.FC<LayoutProps> = ({ children }) => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const { t, i18n } = useTranslation();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  const toggleLanguage = () => {
    const nextLang = i18n.language === 'vi' ? 'en' : 'vi';
    i18n.changeLanguage(nextLang);
  };

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const navItems = [
    { name: t('dashboard.title'), path: '/dashboard', icon: LayoutDashboard },
  ];

  return (
    <div className="min-h-screen bg-[#0b0f19] text-[#f8fafc] font-sans flex">
      {/* 1. Sidebar cho Desktop */}
      <aside className="hidden md:flex flex-col w-64 bg-[#0f172a]/60 backdrop-blur-xl border-r border-white/5 p-6 space-y-8 flex-shrink-0">
        <div className="flex items-center space-x-3">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-[#6366f1] to-[#10b981] flex items-center justify-center font-bold text-xl shadow-lg shadow-indigo-500/20">
            SB
          </div>
          <span className="font-extrabold text-xl bg-gradient-to-r from-white via-slate-200 to-slate-400 bg-clip-text text-transparent">
            SplitBill Pro
          </span>
        </div>

        <nav className="flex-1 space-y-1">
          {navItems.map((item) => {
            const Icon = item.icon;
            const isActive = location.pathname === item.path;
            return (
              <Link
                key={item.path}
                to={item.path}
                className={`flex items-center space-x-3 px-4 py-3 rounded-xl transition-all duration-200 ${
                  isActive 
                    ? 'bg-indigo-600/20 text-indigo-400 border-l-4 border-indigo-500 font-medium' 
                    : 'text-slate-400 hover:bg-white/5 hover:text-slate-200'
                }`}
              >
                <Icon size={20} />
                <span>{item.name}</span>
              </Link>
            );
          })}
        </nav>

        {/* Thông tin User & Nút Đăng xuất ở cuối Sidebar */}
        <div className="pt-6 border-t border-white/5 space-y-4">
          <div className="flex items-center space-x-3">
            <div className="w-10 h-10 rounded-full bg-indigo-500/20 border border-indigo-500/30 flex items-center justify-center font-semibold text-indigo-300">
              {user?.displayName.substring(0, 2).toUpperCase()}
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold truncate">{user?.displayName}</p>
              <p className="text-xs text-slate-400 truncate">{user?.email}</p>
            </div>
          </div>

          <div className="flex space-x-2">
            <button
              onClick={toggleLanguage}
              className="flex-1 flex items-center justify-center space-x-2 py-2 px-3 rounded-lg bg-white/5 hover:bg-white/10 text-xs text-slate-300 transition-colors"
            >
              <Globe size={14} />
              <span>{i18n.language.toUpperCase()}</span>
            </button>
            <button
              onClick={handleLogout}
              className="flex items-center justify-center p-2 rounded-lg bg-rose-500/10 hover:bg-rose-500/20 text-rose-400 transition-colors"
              title={t('common.logout')}
            >
              <LogOut size={16} />
            </button>
          </div>
        </div>
      </aside>

      {/* 2. Main Content Area */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Header di động */}
        <header className="md:hidden flex items-center justify-between p-4 bg-[#0f172a]/60 backdrop-blur-xl border-b border-white/5">
          <div className="flex items-center space-x-2">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-tr from-[#6366f1] to-[#10b981] flex items-center justify-center font-bold text-base">
              SB
            </div>
            <span className="font-bold text-lg">SplitBill Pro</span>
          </div>

          <button 
            onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            className="p-2 text-slate-400 hover:text-slate-200"
          >
            {mobileMenuOpen ? <X size={24} /> : <Menu size={24} />}
          </button>
        </header>

        {/* Mobile Menu Backdrop & Content */}
        {mobileMenuOpen && (
          <div className="md:hidden fixed inset-0 z-50 bg-[#0b0f19] flex flex-col p-6 space-y-6 pt-20">
            <button 
              onClick={() => setMobileMenuOpen(false)}
              className="absolute top-4 right-4 p-2 text-slate-400"
            >
              <X size={24} />
            </button>

            <nav className="flex-1 space-y-2">
              {navItems.map((item) => {
                const Icon = item.icon;
                return (
                  <Link
                    key={item.path}
                    to={item.path}
                    onClick={() => setMobileMenuOpen(false)}
                    className="flex items-center space-x-3 px-4 py-4 rounded-xl bg-white/5 hover:bg-white/10"
                  >
                    <Icon size={22} />
                    <span className="text-lg">{item.name}</span>
                  </Link>
                );
              })}
            </nav>

            <div className="border-t border-white/5 pt-6 space-y-4">
              <div className="flex items-center space-x-3">
                <div className="w-12 h-12 rounded-full bg-indigo-500/20 flex items-center justify-center font-semibold text-indigo-300">
                  {user?.displayName.substring(0, 2).toUpperCase()}
                </div>
                <div>
                  <p className="font-semibold">{user?.displayName}</p>
                  <p className="text-sm text-slate-400">{user?.email}</p>
                </div>
              </div>

              <div className="flex space-x-2">
                <button
                  onClick={toggleLanguage}
                  className="flex-1 flex items-center justify-center space-x-2 py-3 rounded-xl bg-white/5 text-slate-300"
                >
                  <Globe size={18} />
                  <span>{i18n.language === 'vi' ? 'Tiếng Việt (VI)' : 'English (EN)'}</span>
                </button>
                <button
                  onClick={handleLogout}
                  className="flex-1 flex items-center justify-center space-x-2 py-3 rounded-xl bg-rose-500/20 text-rose-400"
                >
                  <LogOut size={18} />
                  <span>{t('common.logout')}</span>
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Header phụ cho Desktop (dùng để chuyển ngôn ngữ / logout nhanh) */}
        <header className="hidden md:flex items-center justify-end px-8 py-4 border-b border-white/5">
          <div className="flex items-center space-x-4">
            <button
              onClick={toggleLanguage}
              className="flex items-center space-x-1 py-1.5 px-3 rounded-lg bg-white/5 hover:bg-white/10 text-xs transition-colors"
            >
              <Globe size={14} />
              <span>{i18n.language === 'vi' ? 'Tiếng Việt' : 'English'}</span>
            </button>
            <span className="text-slate-500">|</span>
            <span className="text-sm font-medium text-slate-300">{user?.displayName}</span>
          </div>
        </header>

        {/* Viewport chính của Page */}
        <main className="flex-1 overflow-y-auto p-4 md:p-8">
          {children}
        </main>
      </div>
    </div>
  );
};

export default Layout;
