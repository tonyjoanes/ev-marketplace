import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';

// Types matching backend
export interface Seller {
  id: string;
  name: string;
  email: string;
  type: 'Private' | 'Dealer';
  subscriptionTier: 'Free' | 'Pro' | 'Premium' | 'Enterprise';
  isVerified: boolean;
  currentListingCount?: number;
  canAddListing?: boolean;
  remainingListings?: number;
}

export interface AuthState {
  token: string | null;
  refreshToken: string | null;
  seller: Seller | null;
  expiresAt: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
}

export interface AuthContextType extends AuthState {
  login: (email: string, password: string) => Promise<void>;
  register: (data: RegisterData) => Promise<void>;
  logout: () => void;
  updateSeller: (seller: Seller) => void;
}

export interface RegisterData {
  email: string;
  password: string;
  name: string;
  type: 'Private' | 'Dealer';
  companyName?: string;
  location?: string;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({
    token: null,
    refreshToken: null,
    seller: null,
    expiresAt: null,
    isAuthenticated: false,
    isLoading: true,
  });

  // Load auth state from localStorage on mount
  useEffect(() => {
    const loadAuthState = () => {
      try {
        const token = localStorage.getItem('auth_token');
        const refreshToken = localStorage.getItem('refresh_token');
        const sellerJson = localStorage.getItem('seller');
        const expiresAt = localStorage.getItem('expires_at');

        if (token && sellerJson && expiresAt) {
          const seller = JSON.parse(sellerJson);
          const expiryDate = new Date(expiresAt);

          // Check if token is expired
          if (expiryDate > new Date()) {
            setState({
              token,
              refreshToken,
              seller,
              expiresAt,
              isAuthenticated: true,
              isLoading: false,
            });
            return;
          }
        }

        // Clear invalid auth state
        localStorage.removeItem('auth_token');
        localStorage.removeItem('refresh_token');
        localStorage.removeItem('seller');
        localStorage.removeItem('expires_at');

        setState({
          token: null,
          refreshToken: null,
          seller: null,
          expiresAt: null,
          isAuthenticated: false,
          isLoading: false,
        });
      } catch (error) {
        console.error('Failed to load auth state:', error);
        setState((prev) => ({ ...prev, isLoading: false }));
      }
    };

    loadAuthState();
  }, []);

  const login = async (email: string, password: string) => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email, password }),
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || 'Login failed');
      }

      const data = await response.json();

      // Store in localStorage
      localStorage.setItem('auth_token', data.token);
      localStorage.setItem('refresh_token', data.refreshToken);
      localStorage.setItem('seller', JSON.stringify(data.seller));
      localStorage.setItem('expires_at', data.expiresAt);

      // Update state
      setState({
        token: data.token,
        refreshToken: data.refreshToken,
        seller: data.seller,
        expiresAt: data.expiresAt,
        isAuthenticated: true,
        isLoading: false,
      });
    } catch (error) {
      console.error('Login error:', error);
      throw error;
    }
  };

  const register = async (data: RegisterData) => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/register`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          email: data.email,
          password: data.password,
          name: data.name,
          type: data.type,
          companyName: data.companyName || null,
          location: data.location || null,
        }),
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || 'Registration failed');
      }

      const responseData = await response.json();

      // Store in localStorage
      localStorage.setItem('auth_token', responseData.token);
      localStorage.setItem('refresh_token', responseData.refreshToken);
      localStorage.setItem('seller', JSON.stringify(responseData.seller));
      localStorage.setItem('expires_at', responseData.expiresAt);

      // Update state
      setState({
        token: responseData.token,
        refreshToken: responseData.refreshToken,
        seller: responseData.seller,
        expiresAt: responseData.expiresAt,
        isAuthenticated: true,
        isLoading: false,
      });
    } catch (error) {
      console.error('Registration error:', error);
      throw error;
    }
  };

  const logout = () => {
    // Clear localStorage
    localStorage.removeItem('auth_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('seller');
    localStorage.removeItem('expires_at');

    // Clear state
    setState({
      token: null,
      refreshToken: null,
      seller: null,
      expiresAt: null,
      isAuthenticated: false,
      isLoading: false,
    });
  };

  const updateSeller = (seller: Seller) => {
    localStorage.setItem('seller', JSON.stringify(seller));
    setState((prev) => ({ ...prev, seller }));
  };

  return (
    <AuthContext.Provider
      value={{
        ...state,
        login,
        register,
        logout,
        updateSeller,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}

// Helper hook for authenticated API calls
export function useAuthenticatedFetch() {
  const { token } = useAuth();

  return async (url: string, options: RequestInit = {}) => {
    const headers = {
      ...options.headers,
      'Content-Type': 'application/json',
      ...(token && { Authorization: `Bearer ${token}` }),
    };

    const response = await fetch(`${API_BASE_URL}${url}`, {
      ...options,
      headers,
    });

    if (!response.ok) {
      if (response.status === 401) {
        // Token expired or invalid
        throw new Error('Unauthorized');
      }
      const error = await response.json();
      throw new Error(error.error || 'Request failed');
    }

    return response;
  };
}
