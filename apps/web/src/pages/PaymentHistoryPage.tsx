import { useState, useEffect } from 'react';
import { useAuth, useAuthenticatedFetch } from '@ev-marketplace/shared';

export interface Payment {
  id: string;
  sellerId: string;
  type: PaymentType;
  amountGbp: number;
  status: PaymentStatus;
  stripePaymentIntentId: string;
  subscriptionId?: string;
  featuredListingId?: string;
  description: string;
  createdAt: string;
  updatedAt: string;
  paidAt?: string;
}

export enum PaymentType {
  Subscription = 0,
  FeaturedListing = 1,
  SingleListing = 2,
  LeadPurchase = 3,
}

export enum PaymentStatus {
  Pending = 0,
  Processing = 1,
  Succeeded = 2,
  Failed = 3,
  Canceled = 4,
  Refunded = 5,
}

interface PaymentStats {
  periodStart: string;
  periodEnd: string;
  totalRevenue: number;
  totalPayments: number;
  successfulPayments: number;
  failedPayments: number;
  averagePaymentAmount: number;
  successRate: number;
  revenueByType: Record<string, number>;
}

export function PaymentHistoryPage() {
  const { seller } = useAuth();
  const authenticatedFetch = useAuthenticatedFetch();

  const [payments, setPayments] = useState<Payment[]>([]);
  const [stats, setStats] = useState<PaymentStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [filterType, setFilterType] = useState<PaymentType | 'all'>('all');
  const [filterStatus, setFilterStatus] = useState<PaymentStatus | 'all'>('all');

  useEffect(() => {
    if (seller) {
      fetchPayments();
      fetchStats();
    }
  }, [seller]);

  const fetchPayments = async () => {
    if (!seller) return;

    try {
      setLoading(true);
      const response = await authenticatedFetch(`/api/payments/seller/${seller.id}`);

      if (!response.ok) {
        throw new Error('Failed to fetch payments');
      }

      const data = await response.json();
      setPayments(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load payments');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const fetchStats = async () => {
    try {
      // Fetch stats for the last 12 months
      const from = new Date();
      from.setMonth(from.getMonth() - 12);
      const to = new Date();

      const response = await authenticatedFetch(
        `/api/payments/stats?from=${from.toISOString()}&to=${to.toISOString()}`
      );

      if (!response.ok) {
        throw new Error('Failed to fetch payment stats');
      }

      const data = await response.json();
      setStats(data);
    } catch (err) {
      console.error('Failed to load payment stats:', err);
    }
  };

  const getStatusBadge = (status: PaymentStatus) => {
    const badges = {
      [PaymentStatus.Pending]: 'bg-yellow-500/20 text-yellow-400 border-yellow-500/50',
      [PaymentStatus.Processing]: 'bg-blue-500/20 text-blue-400 border-blue-500/50',
      [PaymentStatus.Succeeded]: 'bg-green-500/20 text-green-400 border-green-500/50',
      [PaymentStatus.Failed]: 'bg-red-500/20 text-red-400 border-red-500/50',
      [PaymentStatus.Canceled]: 'bg-gray-500/20 text-gray-400 border-gray-500/50',
      [PaymentStatus.Refunded]: 'bg-purple-500/20 text-purple-400 border-purple-500/50',
    };

    const labels = {
      [PaymentStatus.Pending]: 'Pending',
      [PaymentStatus.Processing]: 'Processing',
      [PaymentStatus.Succeeded]: 'Succeeded',
      [PaymentStatus.Failed]: 'Failed',
      [PaymentStatus.Canceled]: 'Canceled',
      [PaymentStatus.Refunded]: 'Refunded',
    };

    return (
      <span className={`px-2 py-1 text-xs font-medium rounded-md border ${badges[status]}`}>
        {labels[status]}
      </span>
    );
  };

  const getTypeLabel = (type: PaymentType) => {
    const labels = {
      [PaymentType.Subscription]: 'Subscription',
      [PaymentType.FeaturedListing]: 'Featured Listing',
      [PaymentType.SingleListing]: 'Single Listing',
      [PaymentType.LeadPurchase]: 'Lead Purchase',
    };
    return labels[type];
  };

  const getTypeIcon = (type: PaymentType) => {
    switch (type) {
      case PaymentType.Subscription:
        return (
          <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z" />
          </svg>
        );
      case PaymentType.FeaturedListing:
        return (
          <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
          </svg>
        );
      case PaymentType.SingleListing:
        return (
          <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
        );
      case PaymentType.LeadPurchase:
        return (
          <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
          </svg>
        );
    }
  };

  const filteredPayments = payments.filter((payment) => {
    if (filterType !== 'all' && payment.type !== filterType) return false;
    if (filterStatus !== 'all' && payment.status !== filterStatus) return false;
    return true;
  });

  const totalSpent = filteredPayments
    .filter((p) => p.status === PaymentStatus.Succeeded)
    .reduce((sum, p) => sum + p.amountGbp, 0);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500 mx-auto"></div>
          <p className="mt-4 text-gray-400">Loading payment history...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-white">Payment History</h1>
        <p className="text-gray-400 mt-2">View all your transactions and payment details</p>
      </div>

      {error && (
        <div className="bg-red-500/10 border border-red-500/50 rounded-lg p-4">
          <p className="text-red-400">{error}</p>
        </div>
      )}

      {/* Stats Cards */}
      {stats && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <div className="card">
            <div className="flex items-center justify-between mb-2">
              <p className="text-sm text-gray-500">Total Spent (12mo)</p>
              <svg className="w-5 h-5 text-primary-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <p className="text-3xl font-bold text-gradient">£{stats.totalRevenue.toLocaleString()}</p>
          </div>

          <div className="card">
            <div className="flex items-center justify-between mb-2">
              <p className="text-sm text-gray-500">Total Payments</p>
              <svg className="w-5 h-5 text-blue-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </div>
            <p className="text-3xl font-bold text-white">{stats.totalPayments}</p>
          </div>

          <div className="card">
            <div className="flex items-center justify-between mb-2">
              <p className="text-sm text-gray-500">Success Rate</p>
              <svg className="w-5 h-5 text-green-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <p className="text-3xl font-bold text-white">{stats.successRate.toFixed(1)}%</p>
          </div>

          <div className="card">
            <div className="flex items-center justify-between mb-2">
              <p className="text-sm text-gray-500">Average Payment</p>
              <svg className="w-5 h-5 text-purple-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 12l3-3 3 3 4-4M8 21l4-4 4 4M3 4h18M4 4h16v12a1 1 0 01-1 1H5a1 1 0 01-1-1V4z" />
              </svg>
            </div>
            <p className="text-3xl font-bold text-white">£{stats.averagePaymentAmount.toFixed(2)}</p>
          </div>
        </div>
      )}

      {/* Filters */}
      <div className="card">
        <div className="flex flex-wrap items-center gap-4">
          <div className="flex-1 min-w-[200px]">
            <label className="block text-sm font-medium text-gray-300 mb-2">Filter by Type</label>
            <select
              value={filterType}
              onChange={(e) => setFilterType(e.target.value === 'all' ? 'all' : Number(e.target.value) as PaymentType)}
              className="w-full px-4 py-2 bg-dark-800 border border-dark-700 rounded-lg text-white focus:border-primary-500 focus:ring-1 focus:ring-primary-500"
            >
              <option value="all">All Types</option>
              <option value={PaymentType.Subscription}>Subscription</option>
              <option value={PaymentType.FeaturedListing}>Featured Listing</option>
              <option value={PaymentType.SingleListing}>Single Listing</option>
              <option value={PaymentType.LeadPurchase}>Lead Purchase</option>
            </select>
          </div>

          <div className="flex-1 min-w-[200px]">
            <label className="block text-sm font-medium text-gray-300 mb-2">Filter by Status</label>
            <select
              value={filterStatus}
              onChange={(e) => setFilterStatus(e.target.value === 'all' ? 'all' : Number(e.target.value) as PaymentStatus)}
              className="w-full px-4 py-2 bg-dark-800 border border-dark-700 rounded-lg text-white focus:border-primary-500 focus:ring-1 focus:ring-primary-500"
            >
              <option value="all">All Statuses</option>
              <option value={PaymentStatus.Pending}>Pending</option>
              <option value={PaymentStatus.Processing}>Processing</option>
              <option value={PaymentStatus.Succeeded}>Succeeded</option>
              <option value={PaymentStatus.Failed}>Failed</option>
              <option value={PaymentStatus.Canceled}>Canceled</option>
              <option value={PaymentStatus.Refunded}>Refunded</option>
            </select>
          </div>

          <div className="flex-1 min-w-[200px]">
            <label className="block text-sm font-medium text-gray-300 mb-2">&nbsp;</label>
            <div className="text-sm text-gray-400">
              Showing {filteredPayments.length} of {payments.length} payments
            </div>
          </div>
        </div>
      </div>

      {/* Payments List */}
      {filteredPayments.length === 0 ? (
        <div className="card text-center py-12">
          <div className="w-16 h-16 bg-dark-800 rounded-full flex items-center justify-center mx-auto mb-4">
            <svg
              className="w-8 h-8 text-gray-500"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"
              />
            </svg>
          </div>
          <h2 className="text-xl font-bold text-white mb-2">No payments found</h2>
          <p className="text-gray-400">
            {payments.length === 0
              ? 'Your payment history will appear here'
              : 'Try adjusting your filters'}
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {filteredPayments.map((payment) => (
            <div
              key={payment.id}
              className="card hover:border-primary-500/50 transition-all"
            >
              <div className="flex items-start justify-between gap-4">
                {/* Payment Details */}
                <div className="flex items-start gap-4 flex-1">
                  <div className="w-10 h-10 bg-dark-800 rounded-lg flex items-center justify-center text-primary-500">
                    {getTypeIcon(payment.type)}
                  </div>

                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1">
                      <h3 className="text-lg font-semibold text-white">
                        {getTypeLabel(payment.type)}
                      </h3>
                      {getStatusBadge(payment.status)}
                    </div>
                    <p className="text-sm text-gray-400 mb-2">{payment.description}</p>
                    <div className="flex items-center gap-4 text-xs text-gray-500">
                      <span>
                        Created: {new Date(payment.createdAt).toLocaleDateString('en-GB', {
                          day: '2-digit',
                          month: 'short',
                          year: 'numeric',
                          hour: '2-digit',
                          minute: '2-digit'
                        })}
                      </span>
                      {payment.paidAt && (
                        <span>
                          Paid: {new Date(payment.paidAt).toLocaleDateString('en-GB', {
                            day: '2-digit',
                            month: 'short',
                            year: 'numeric',
                            hour: '2-digit',
                            minute: '2-digit'
                          })}
                        </span>
                      )}
                    </div>
                  </div>
                </div>

                {/* Amount */}
                <div className="text-right">
                  <p className={`text-2xl font-bold ${
                    payment.status === PaymentStatus.Succeeded
                      ? 'text-gradient'
                      : payment.status === PaymentStatus.Failed
                      ? 'text-red-400'
                      : 'text-gray-400'
                  }`}>
                    £{payment.amountGbp.toLocaleString()}
                  </p>
                  <p className="text-xs text-gray-500 mt-1">
                    ID: {payment.id.substring(0, 8)}...
                  </p>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Summary Footer */}
      {filteredPayments.length > 0 && (
        <div className="card bg-gradient-to-r from-primary-900/20 to-accent-900/20">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-400">Total from filtered payments</p>
              <p className="text-2xl font-bold text-gradient mt-1">
                £{totalSpent.toLocaleString()}
              </p>
            </div>
            <div className="text-right">
              <p className="text-sm text-gray-400">Successful payments</p>
              <p className="text-2xl font-bold text-white mt-1">
                {filteredPayments.filter(p => p.status === PaymentStatus.Succeeded).length}
              </p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
