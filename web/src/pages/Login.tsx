import React, { useState, useEffect } from 'react';
import { Link, useNavigate, useSearchParams, useLocation } from 'react-router-dom';
import {
  ChartBarIcon,
  EyeIcon,
  EyeSlashIcon,
  ExclamationCircleIcon,
  CheckCircleIcon,
} from '@heroicons/react/24/outline';
import { UserService, LoginRequest } from '../auth/authService';
import { useAuth } from '../auth/AuthContext';

interface LoginProvider {
  id: string;
  name: string;
  icon: string;
  bgColor: string;
  textColor: string;
  borderColor: string;
  enabled: boolean;
}

interface LoginConfig {
  enableEmailLogin: boolean;
  enableProviders: string[];
  appName: string;
  appLogo?: string;
}

interface LoginForm {
  email: string;
  password: string;
}

const Login: React.FC = () => {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [searchParams] = useSearchParams();
  const location = useLocation();
  const [loading, setLoading] = useState<string | null>(null);
  const [error, setError] = useState<string>('');
  const [success, setSuccess] = useState<string>('');
  const [showPassword, setShowPassword] = useState<boolean>(false);
  const [loginForm, setLoginForm] = useState<LoginForm>({
    email: '',
    password: '',
  });

  // Login configuration - can be obtained from environment variables or API
  const loginConfig: LoginConfig = {
    enableEmailLogin: true,
    enableProviders: ['github', 'microsoft', 'google'], // Configurable enabled providers
    appName: 'Nexova',
  };

  const allProviders: LoginProvider[] = [
    {
      id: 'github',
      name: 'GitHub',
      icon: '🐙',
      bgColor: 'bg-gray-900 hover:bg-gray-800',
      textColor: 'text-white',
      borderColor: 'border-gray-900',
      enabled: loginConfig.enableProviders.includes('github'),
    },
    {
      id: 'microsoft',
      name: 'Microsoft',
      icon: '🪟',
      bgColor: 'bg-blue-600 hover:bg-blue-700',
      textColor: 'text-white',
      borderColor: 'border-blue-600',
      enabled: loginConfig.enableProviders.includes('microsoft'),
    },
    {
      id: 'google',
      name: 'Google',
      icon: '🌈',
      bgColor: 'bg-white hover:bg-gray-50',
      textColor: 'text-gray-900',
      borderColor: 'border-gray-300',
      enabled: loginConfig.enableProviders.includes('google'),
    },
    {
      id: 'gitlab',
      name: 'GitLab',
      icon: '🦊',
      bgColor: 'bg-orange-600 hover:bg-orange-700',
      textColor: 'text-white',
      borderColor: 'border-orange-600',
      enabled: loginConfig.enableProviders.includes('gitlab'),
    },
    {
      id: 'bitbucket',
      name: 'Bitbucket',
      icon: '🪣',
      bgColor: 'bg-blue-500 hover:bg-blue-600',
      textColor: 'text-white',
      borderColor: 'border-blue-500',
      enabled: loginConfig.enableProviders.includes('bitbucket'),
    },
  ];

  const enabledProviders = allProviders.filter(provider => provider.enabled);

  // Check for registration success message
  useEffect(() => {
    const state = location.state as { message?: string };
    if (state?.message) {
      setSuccess(state.message);
      // Clear the state to prevent showing the message again
      window.history.replaceState({}, document.title);
    }
  }, [location.state]);

  const handleEmailLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading('email');
    setError('');
    
    try {
      // Client-side validation
      if (!loginForm.email || !loginForm.password) {
        throw new Error('Please fill in all fields');
      }
      
      if (loginForm.password.length < 6) {
        throw new Error('Password must be at least 6 characters');
      }
      
      // Prepare login data
      const loginData: LoginRequest = {
        email: loginForm.email,
        password: loginForm.password,
      };
      
      // Call real login API
      await UserService.login(loginData);
      
      // Update authentication state
      await login();
      
      // Login successful - cookies are set by the server
      // No need to manually store tokens since using cookies
      console.log('Login successful');
      
      // Get redirect URI from query params, default to home page
      const redirectUri = searchParams.get('redirectUri') || '/';
      
      // Redirect to original page or home page using React Router
      navigate(redirectUri, { replace: true });
    } catch (error: any) {
      console.error('Login error:', error);
      setError(error.message || 'Login failed. Please try again.');
    } finally {
      setLoading(null);
    }
  };

  const handleProviderLogin = async (providerId: string) => {
    setLoading(providerId);
    setError('');
    
    try {
      console.log(`Initiating ${providerId} login`);
      
      // Capitalize provider name for backend API
      const providerName = providerId.charAt(0).toUpperCase() + providerId.slice(1);
      
      if (providerId === 'microsoft') {
        // Get redirect URI from query params, default to home page
        const redirectUri = searchParams.get('redirectUri') || '/';
        
        // Use UserService to initiate OAuth login
        UserService.loginWithProvider(providerName, window.location.origin + redirectUri);
        return;
      } else {
        // Other providers can use the same method when backend supports them
        // For now, show message that they are not yet implemented
        setError(`${providerId} login is not yet implemented. Please use email login.`);
        setLoading(null);
        return;
      }
    } catch (error) {
      console.error(`${providerId} login failed:`, error);
      setError(`Failed to authenticate with ${providerId}. Please try again.`);
      setLoading(null);
    }
  };


  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-indigo-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8 relative overflow-hidden">
      {/* Background Pattern */}
      <div className="absolute inset-0 overflow-hidden">
        {/* Geometric shapes */}
        <div className="absolute top-20 left-10 w-20 h-20 bg-blue-200/20 rounded-full animate-pulse"></div>
        <div className="absolute top-40 right-20 w-16 h-16 bg-indigo-200/15 rounded-full animate-pulse" style={{ animationDelay: '2s' }}></div>
        <div className="absolute bottom-40 left-20 w-12 h-12 bg-purple-200/20 rounded-full animate-pulse" style={{ animationDelay: '1s' }}></div>
        <div className="absolute bottom-20 right-40 w-18 h-18 bg-cyan-200/15 rounded-full animate-pulse" style={{ animationDelay: '3s' }}></div>
        
        {/* Grid pattern */}
        <div className="absolute inset-0 opacity-5">
          <div className="grid grid-cols-12 h-full">
            {Array.from({ length: 12 }).map((_, i) => (
              <div key={i} className="border-r border-gray-300"></div>
            ))}
          </div>
          <div className="absolute inset-0">
            <div className="grid grid-rows-12 h-full">
              {Array.from({ length: 12 }).map((_, i) => (
                <div key={i} className="border-b border-gray-300"></div>
              ))}
            </div>
          </div>
        </div>
        
        {/* Pipeline-like connecting lines */}
        <svg className="absolute inset-0 w-full h-full opacity-10" preserveAspectRatio="none">
          <defs>
            <linearGradient id="lineGradient" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stopColor="#3B82F6" />
              <stop offset="50%" stopColor="#6366F1" />
              <stop offset="100%" stopColor="#8B5CF6" />
            </linearGradient>
          </defs>
          <path
            d="M 0,200 Q 200,100 400,200 T 800,200"
            stroke="url(#lineGradient)"
            strokeWidth="2"
            fill="none"
            className="animate-pulse"
          />
          <path
            d="M 0,400 Q 300,300 600,400 T 1200,400"
            stroke="url(#lineGradient)"
            strokeWidth="2"
            fill="none"
            className="animate-pulse"
            style={{ animationDelay: '1s' }}
          />
          <circle cx="200" cy="200" r="4" fill="#3B82F6" className="animate-ping" />
          <circle cx="600" cy="400" r="4" fill="#6366F1" className="animate-ping" style={{ animationDelay: '2s' }} />
        </svg>
      </div>

      <div className="sm:mx-auto sm:w-full sm:max-w-md relative z-10">
        {/* Logo */}
        <div className="flex justify-center items-center mb-6">
          <div className="h-11 w-11 rounded-lg bg-gray-900 flex items-center justify-center mr-3 shadow-sm">
            <ChartBarIcon className="h-7 w-7 text-white" />
          </div>
          <h1 className="text-3xl font-bold text-accent-700">
            {loginConfig.appName}
          </h1>
        </div>
        
        <h2 className="text-center text-2xl font-medium text-gray-900 mb-2">
          Sign in to your account
        </h2>
        <p className="text-center text-sm text-gray-600 mb-8">
          Explore your dashboards, charts and data
        </p>
      </div>

      <div className="sm:mx-auto sm:w-full sm:max-w-md relative z-10">
        <div className="bg-white/80 backdrop-blur-sm py-8 px-4 shadow-xl rounded-xl sm:px-10 border border-white/20">
          {/* Success Message */}
          {success && (
            <div className="mb-6 p-3 bg-green-50 border border-green-200 rounded-md">
              <div className="flex items-center">
                <CheckCircleIcon className="h-4 w-4 text-green-400 mr-2" />
                <span className="text-sm text-green-800">{success}</span>
              </div>
            </div>
          )}

          {/* Error Message */}
          {error && (
            <div className="mb-6 p-3 bg-red-50 border border-red-200 rounded-md">
              <div className="flex items-center">
                <ExclamationCircleIcon className="h-4 w-4 text-red-400 mr-2" />
                <span className="text-sm text-red-800">{error}</span>
              </div>
            </div>
          )}

          {/* Email Login Form */}
          {loginConfig.enableEmailLogin && (
            <form onSubmit={handleEmailLogin} className="space-y-6">
              <div>
                <label htmlFor="email" className="block text-sm font-medium text-gray-700">
                  Email address
                </label>
                <div className="mt-1">
                  <input
                    id="email"
                    name="email"
                    type="email"
                    autoComplete="email"
                    required
                    value={loginForm.email}
                    onChange={(e) => setLoginForm(prev => ({ ...prev, email: e.target.value }))}
                                         className="appearance-none block w-full px-3 py-2 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-1 focus:ring-gray-400 focus:border-gray-400 sm:text-sm transition-colors"
                    placeholder="Enter your email"
                  />
                </div>
              </div>

              <div>
                <label htmlFor="password" className="block text-sm font-medium text-gray-700">
                  Password
                </label>
                <div className="mt-1 relative">
                  <input
                    id="password"
                    name="password"
                    type={showPassword ? 'text' : 'password'}
                    autoComplete="current-password"
                    required
                    value={loginForm.password}
                    onChange={(e) => setLoginForm(prev => ({ ...prev, password: e.target.value }))}
                                         className="appearance-none block w-full px-3 py-2 pr-10 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-1 focus:ring-gray-400 focus:border-gray-400 sm:text-sm transition-colors"
                    placeholder="Enter your password"
                  />
                  <button
                    type="button"
                    className="absolute inset-y-0 right-0 pr-3 flex items-center"
                    onClick={() => setShowPassword(!showPassword)}
                  >
                    {showPassword ? (
                      <EyeSlashIcon className="h-4 w-4 text-gray-400" />
                    ) : (
                      <EyeIcon className="h-4 w-4 text-gray-400" />
                    )}
                  </button>
                </div>
              </div>

              <div className="flex items-center justify-between">
                <div className="text-sm">
                  <a href="#" className="font-medium text-gray-600 hover:text-gray-900">
                    Forgot your password?
                  </a>
                </div>
              </div>

              <div>
                <button
                  type="submit"
                  disabled={loading === 'email'}
                                     className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-gray-900 hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-400 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200"
                >
                  {loading === 'email' ? (
                    <div className="flex items-center">
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                      Signing in...
                    </div>
                  ) : (
                    'Sign in'
                  )}
                </button>
              </div>
            </form>
          )}

          {/* Divider */}
          {loginConfig.enableEmailLogin && enabledProviders.length > 0 && (
            <div className="mt-6">
              <div className="relative">
                <div className="absolute inset-0 flex items-center">
                  <div className="w-full border-t border-gray-300" />
                </div>
                <div className="relative flex justify-center text-sm">
                  <span className="px-2 bg-white text-gray-500">Or continue with</span>
                </div>
              </div>
            </div>
          )}

          {/* Provider Login Buttons */}
          {enabledProviders.length > 0 && (
            <div className="mt-6 space-y-3">
              {enabledProviders.map((provider) => (
                <button
                  key={provider.id}
                  onClick={() => handleProviderLogin(provider.id)}
                  disabled={loading !== null}
                  className={`w-full flex items-center justify-center px-4 py-2 border ${provider.borderColor} rounded-md text-sm font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed ${provider.bgColor} ${provider.textColor}`}
                >
                  {loading === provider.id ? (
                    <div className="flex items-center">
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2"></div>
                      Connecting...
                    </div>
                  ) : (
                    <div className="flex items-center">
                      <span className="text-lg mr-2">{provider.icon}</span>
                      {provider.name}
                    </div>
                  )}
                </button>
              ))}
            </div>
          )}

          {/* Register Link */}
          <div className="mt-6 text-center">
            <p className="text-sm text-gray-600">
              Don't have an account?{' '}
              <Link
                to="/register"
                className="font-medium text-gray-900 hover:text-gray-700 transition-colors"
              >
                Create one here
              </Link>
            </p>
          </div>
        </div>

        {/* Footer */}
        <div className="mt-8 text-center relative z-10">
          <p className="text-xs text-gray-500">
            By signing in, you agree to our{' '}
            <a href="#" className="text-blue-600 hover:text-blue-800 transition-colors">Terms</a>
            {' '}and{' '}
            <a href="#" className="text-blue-600 hover:text-blue-800 transition-colors">Privacy Policy</a>
          </p>
        </div>
      </div>
    </div>
  );
};

export default Login;
