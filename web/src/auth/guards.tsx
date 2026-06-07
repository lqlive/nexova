import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from './AuthContext';

const LoadingScreen: React.FC = () => (
  <div className="min-h-screen flex items-center justify-center">
    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-gray-900"></div>
  </div>
);

// Redirects unauthenticated users to the login page.
export const RequireAuth: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) return <LoadingScreen />;

  if (!isAuthenticated) {
    const redirectUri = encodeURIComponent(location.pathname + location.search);
    return <Navigate to={`/login?redirectUri=${redirectUri}`} replace />;
  }

  return <>{children}</>;
};

// Prevents authenticated users from visiting auth pages.
export const RequireNoAuth: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) return <LoadingScreen />;

  if (isAuthenticated) return <Navigate to="/" replace />;

  return <>{children}</>;
};
