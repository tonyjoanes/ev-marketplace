import { Link, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '@ev-marketplace/shared';

export function DashboardLayout() {
  const { seller, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  return (
    <div className="min-h-screen bg-dark-950">
      {/* Top Navigation */}
      <nav className="bg-dark-900 border-b border-dark-800">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex">
              {/* Logo */}
              <Link to="/" className="flex items-center text-xl font-bold text-gradient">
                EV Marketplace
              </Link>

              {/* Dashboard Nav */}
              <div className="hidden sm:ml-8 sm:flex sm:space-x-8">
                <Link
                  to="/dashboard"
                  className="inline-flex items-center px-1 pt-1 border-b-2 border-transparent hover:border-primary-500 text-gray-300 hover:text-white transition-colors"
                >
                  Overview
                </Link>
                <Link
                  to="/dashboard/listings"
                  className="inline-flex items-center px-1 pt-1 border-b-2 border-transparent hover:border-primary-500 text-gray-300 hover:text-white transition-colors"
                >
                  My Listings
                </Link>
                <Link
                  to="/dashboard/subscription"
                  className="inline-flex items-center px-1 pt-1 border-b-2 border-transparent hover:border-primary-500 text-gray-300 hover:text-white transition-colors"
                >
                  Subscription
                </Link>
                <Link
                  to="/dashboard/payments"
                  className="inline-flex items-center px-1 pt-1 border-b-2 border-transparent hover:border-primary-500 text-gray-300 hover:text-white transition-colors"
                >
                  Payments
                </Link>
              </div>
            </div>

            {/* User Menu */}
            <div className="flex items-center space-x-4">
              <div className="text-sm">
                <p className="text-white font-medium">{seller?.name}</p>
                <p className="text-gray-400 text-xs">
                  {seller?.subscriptionTier} • {seller?.type}
                </p>
              </div>
              <button
                onClick={handleLogout}
                className="text-gray-400 hover:text-white text-sm"
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <Outlet />
      </main>
    </div>
  );
}

export function DashboardOverviewPage() {
  const { seller } = useAuth();

  return (
    <div className="space-y-6">
      {/* Welcome Header */}
      <div className="card">
        <h1 className="text-2xl font-bold text-white">
          Welcome back, {seller?.name}!
        </h1>
        <p className="text-gray-400 mt-2">
          Manage your listings, subscription, and account settings
        </p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Listings */}
        <div className="card">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-gray-400 text-sm">Active Listings</p>
              <p className="text-3xl font-bold text-white mt-2">
                {seller?.currentListingCount || 0}
              </p>
            </div>
            <div className="w-12 h-12 bg-primary-500/20 rounded-lg flex items-center justify-center">
              <svg className="w-6 h-6 text-primary-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </div>
          </div>
          {seller?.remainingListings !== undefined && (
            <p className="text-sm text-gray-500 mt-4">
              {seller.remainingListings === Number.MAX_SAFE_INTEGER
                ? 'Unlimited remaining'
                : `${seller.remainingListings} remaining`}
            </p>
          )}
        </div>

        {/* Subscription */}
        <div className="card">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-gray-400 text-sm">Subscription</p>
              <p className="text-3xl font-bold text-gradient mt-2">
                {seller?.subscriptionTier}
              </p>
            </div>
            <div className="w-12 h-12 bg-accent-500/20 rounded-lg flex items-center justify-center">
              <svg className="w-6 h-6 text-accent-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z" />
              </svg>
            </div>
          </div>
          <Link
            to="/dashboard/subscription"
            className="text-sm text-primary-500 hover:text-primary-400 mt-4 inline-block"
          >
            Manage subscription →
          </Link>
        </div>

        {/* Account Status */}
        <div className="card">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-gray-400 text-sm">Account Status</p>
              <p className="text-3xl font-bold text-white mt-2">
                {seller?.isVerified ? (
                  <span className="text-green-500">Verified</span>
                ) : (
                  <span className="text-yellow-500">Pending</span>
                )}
              </p>
            </div>
            <div className="w-12 h-12 bg-green-500/20 rounded-lg flex items-center justify-center">
              <svg className="w-6 h-6 text-green-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
          </div>
          {!seller?.isVerified && (
            <p className="text-sm text-gray-500 mt-4">
              Verification pending
            </p>
          )}
        </div>
      </div>

      {/* Quick Actions */}
      <div className="card">
        <h2 className="text-xl font-bold text-white mb-4">Quick Actions</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <Link
            to="/dashboard/listings/new"
            className="flex items-center space-x-3 p-4 bg-dark-800 rounded-lg hover:bg-dark-700 transition-colors"
          >
            <svg className="w-6 h-6 text-primary-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            <span className="text-white">Create Listing</span>
          </Link>

          <Link
            to="/dashboard/listings"
            className="flex items-center space-x-3 p-4 bg-dark-800 rounded-lg hover:bg-dark-700 transition-colors"
          >
            <svg className="w-6 h-6 text-primary-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 10h16M4 14h16M4 18h16" />
            </svg>
            <span className="text-white">View Listings</span>
          </Link>

          <Link
            to="/dashboard/subscription"
            className="flex items-center space-x-3 p-4 bg-dark-800 rounded-lg hover:bg-dark-700 transition-colors"
          >
            <svg className="w-6 h-6 text-accent-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
            </svg>
            <span className="text-white">Upgrade Plan</span>
          </Link>

          <Link
            to="/dashboard/payments"
            className="flex items-center space-x-3 p-4 bg-dark-800 rounded-lg hover:bg-dark-700 transition-colors"
          >
            <svg className="w-6 h-6 text-primary-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
            </svg>
            <span className="text-white">Payment History</span>
          </Link>
        </div>
      </div>
    </div>
  );
}
