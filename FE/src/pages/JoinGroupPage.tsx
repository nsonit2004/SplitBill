import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import api from '../services/api';

const JoinGroupPage: React.FC = () => {
  const { token } = useAuth();
  const navigate = useNavigate();
  const { token: inviteToken } = useParams<{ token: string }>();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const acceptInvite = async () => {
      if (!inviteToken) {
        setError('Liên kết mời không hợp lệ.');
        setLoading(false);
        return;
      }

      if (!token) {
        navigate(`/login?inviteToken=${encodeURIComponent(inviteToken)}`, { replace: true });
        return;
      }

      try {
        const response = await api.post(`/groups/invites/${inviteToken}/accept`);
        const groupId = response.data?.id;
        if (groupId) {
          navigate(`/groups/${groupId}`, { replace: true });
          return;
        }
        navigate('/dashboard', { replace: true });
      } catch (err: any) {
        setError(err.response?.data?.message || 'Không thể tham gia nhóm từ lời mời này.');
        setLoading(false);
      }
    };

    acceptInvite();
  }, [inviteToken, navigate, token]);

  return (
    <div className="min-h-screen bg-[#0b0f19] flex items-center justify-center p-4">
      <div className="w-full max-w-md rounded-2xl border border-white/10 bg-[#0f172a]/70 p-6 text-center">
        {loading ? (
          <div className="space-y-3">
            <div className="mx-auto h-8 w-8 animate-spin rounded-full border-4 border-indigo-500 border-t-transparent" />
            <p className="text-sm text-slate-300">Đang tham gia nhóm...</p>
          </div>
        ) : (
          <div className="space-y-4">
            <p className="text-sm text-rose-400">{error}</p>
            <button
              onClick={() => navigate('/dashboard')}
              className="rounded-xl bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-500"
            >
              Về Dashboard
            </button>
          </div>
        )}
      </div>
    </div>
  );
};

export default JoinGroupPage;
