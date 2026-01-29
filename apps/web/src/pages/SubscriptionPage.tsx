import { useState, useEffect } from 'react';
import { useAuth, useAuthenticatedFetch } from '@ev-marketplace/shared';

interface SubscriptionPlan {
  tier: string;
  name: string;
  description: string;
  priceGbpPerMonth: number;
  maxListings: string;
  featuredPlacementRotation: boolean;
  priorityPlacement: boolean;
  analyticsDashboard: boolean;
  apiAccess: boolean;
  dedicatedSupport: boolean;
  leadsPerMonth: string;
}

export function SubscriptionPage() {
  const { seller, updateSeller } = useAuth();
  const authenticatedFetch = useAuthenticatedFetch();

  const [plans, setPlans] = useState<SubscriptionPlan[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [processingTier, setProcessingTier] = useState<string | null>(null);

  useEffect(() => {
    fetchPlans();
  }, []);

  const fetchPlans = async () => {
    try {
      setLoading(true);
      const response = await authenticatedFetch('/api/subscriptions/plans');
      const data = await response.json();
      setPlans(data);
    } catch (err) {
      setError('Failed to load subscription plans');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleUpgrade = async (tier: string) => {
    if (!seller) return;

    try {
      setProcessingTier(tier);
      setError('');

      const response = await authenticatedFetch('/api/subscriptions/checkout', {
        method: 'POST',
        body: JSON.stringify({
          sellerId: seller.id,
          tier: tier,
          successUrl: `${window.location.origin}/dashboard/subscription?success=true`,
          cancelUrl: `${window.location.origin}/dashboard/subscription?canceled=true`,
        }),
      });

      const data = await response.json();

      if (data.url) {
        // Redirect to Stripe Checkout
        window.location.href = data.url;
      } else {
        throw new Error('No checkout URL received');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to start checkout');
      setProcessingTier(null);
    }
  };

  const handleManageBilling = async () => {
    if (!seller) return;

    try {
      setProcessingTier('portal');
      setError('');

      const response = await authenticatedFetch('/api/subscriptions/portal', {
        method: 'POST',
        body: JSON.stringify({
          sellerId: seller.id,
          returnUrl: `${window.location.origin}/dashboard/subscription`,
        }),
      });

      const data = await response.json();

      if (data.url) {
        // Redirect to Stripe Billing Portal
        window.location.href = data.url;
      } else {
        throw new Error('No portal URL received');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to open billing portal');
      setProcessingTier(null);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500 mx-auto"></div>
          <p className="mt-4 text-gray-400">Loading subscription plans...</p>
        </div>
      </div>
    );
  }

  const currentPlan = plans.find((p) => p.tier === seller?.subscriptionTier);

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="card">
        <h1 className="text-3xl font-bold text-white">Subscription Management</h1>
        <p className="text-gray-400 mt-2">
          Choose the plan that fits your business needs
        </p>
      </div>

      {/* Success/Cancel Messages */}
      {new URLSearchParams(window.location.search).get('success') && (
        <div className="bg-green-500/10 border border-green-500/50 rounded-lg p-4">
          <p className="text-green-400">
            ✓ Subscription updated successfully! Your new features are now active.
          </p>
        </div>
      )}

      {new URLSearchParams(window.location.search).get('canceled') && (
        <div className="bg-yellow-500/10 border border-yellow-500/50 rounded-lg p-4">
          <p className="text-yellow-400">
            Checkout was canceled. Your subscription remains unchanged.
          </p>
        </div>
      )}

      {error && (
        <div className="bg-red-500/10 border border-red-500/50 rounded-lg p-4">
          <p className="text-red-400">{error}</p>
        </div>
      )}

      {/* Current Subscription */}
      {currentPlan && (
        <div className="card bg-gradient-to-r from-primary-900/20 to-accent-900/20 border-primary-500/30">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-400 mb-2">Current Plan</p>
              <h2 className="text-2xl font-bold text-gradient">{currentPlan.name}</h2>
              <p className="text-gray-300 mt-1">{currentPlan.description}</p>
              <p className="text-3xl font-bold text-white mt-4">
                {currentPlan.priceGbpPerMonth > 0
                  ? `£${currentPlan.priceGbpPerMonth}/month`
                  : 'Free'}
              </p>
            </div>
            {seller?.subscriptionTier !== 'Free' && (
              <button
                onClick={handleManageBilling}
                disabled={processingTier === 'portal'}
                className="btn bg-dark-800 hover:bg-dark-700 text-white disabled:opacity-50"
              >
                {processingTier === 'portal' ? 'Loading...' : 'Manage Billing'}
              </button>
            )}
          </div>

          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-6 pt-6 border-t border-dark-700">
            <div>
              <p className="text-sm text-gray-400">Listings</p>
              <p className="text-lg font-semibold text-white">{currentPlan.maxListings}</p>
            </div>
            <div>
              <p className="text-sm text-gray-400">Leads/Month</p>
              <p className="text-lg font-semibold text-white">{currentPlan.leadsPerMonth}</p>
            </div>
            <div>
              <p className="text-sm text-gray-400">Analytics</p>
              <p className="text-lg font-semibold text-white">
                {currentPlan.analyticsDashboard ? '✓ Yes' : '✗ No'}
              </p>
            </div>
            <div>
              <p className="text-sm text-gray-400">Priority Support</p>
              <p className="text-lg font-semibold text-white">
                {currentPlan.dedicatedSupport ? '✓ Yes' : '✗ No'}
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Available Plans */}
      <div>
        <h2 className="text-2xl font-bold text-white mb-6">Available Plans</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {plans.map((plan) => {
            const isCurrentPlan = plan.tier === seller?.subscriptionTier;
            const canUpgrade = !isCurrentPlan;

            return (
              <div
                key={plan.tier}
                className={`card ${
                  isCurrentPlan
                    ? 'border-primary-500 bg-primary-900/10'
                    : 'hover:border-primary-500/50'
                } transition-all`}
              >
                {/* Plan Header */}
                <div className="text-center pb-6 border-b border-dark-700">
                  <h3 className="text-xl font-bold text-white">{plan.name}</h3>
                  <p className="text-gray-400 text-sm mt-2">{plan.description}</p>
                  <div className="mt-4">
                    <span className="text-4xl font-bold text-white">
                      {plan.priceGbpPerMonth > 0 ? `£${plan.priceGbpPerMonth}` : 'Free'}
                    </span>
                    {plan.priceGbpPerMonth > 0 && (
                      <span className="text-gray-400 text-sm">/month</span>
                    )}
                  </div>
                </div>

                {/* Features */}
                <ul className="space-y-3 py-6">
                  <li className="flex items-start">
                    <svg
                      className="w-5 h-5 text-primary-500 mr-2 flex-shrink-0 mt-0.5"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M5 13l4 4L19 7"
                      />
                    </svg>
                    <span className="text-gray-300 text-sm">
                      <strong>{plan.maxListings}</strong> listings
                    </span>
                  </li>
                  <li className="flex items-start">
                    <svg
                      className="w-5 h-5 text-primary-500 mr-2 flex-shrink-0 mt-0.5"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M5 13l4 4L19 7"
                      />
                    </svg>
                    <span className="text-gray-300 text-sm">
                      <strong>{plan.leadsPerMonth}</strong> leads/month
                    </span>
                  </li>
                  {plan.featuredPlacementRotation && (
                    <li className="flex items-start">
                      <svg
                        className="w-5 h-5 text-primary-500 mr-2 flex-shrink-0 mt-0.5"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M5 13l4 4L19 7"
                        />
                      </svg>
                      <span className="text-gray-300 text-sm">Featured placement</span>
                    </li>
                  )}
                  {plan.priorityPlacement && (
                    <li className="flex items-start">
                      <svg
                        className="w-5 h-5 text-primary-500 mr-2 flex-shrink-0 mt-0.5"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M5 13l4 4L19 7"
                        />
                      </svg>
                      <span className="text-gray-300 text-sm">Priority placement</span>
                    </li>
                  )}
                  {plan.analyticsDashboard && (
                    <li className="flex items-start">
                      <svg
                        className="w-5 h-5 text-primary-500 mr-2 flex-shrink-0 mt-0.5"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M5 13l4 4L19 7"
                        />
                      </svg>
                      <span className="text-gray-300 text-sm">Analytics dashboard</span>
                    </li>
                  )}
                  {plan.apiAccess && (
                    <li className="flex items-start">
                      <svg
                        className="w-5 h-5 text-primary-500 mr-2 flex-shrink-0 mt-0.5"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M5 13l4 4L19 7"
                        />
                      </svg>
                      <span className="text-gray-300 text-sm">API access</span>
                    </li>
                  )}
                  {plan.dedicatedSupport && (
                    <li className="flex items-start">
                      <svg
                        className="w-5 h-5 text-primary-500 mr-2 flex-shrink-0 mt-0.5"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M5 13l4 4L19 7"
                        />
                      </svg>
                      <span className="text-gray-300 text-sm">Dedicated support</span>
                    </li>
                  )}
                </ul>

                {/* Action Button */}
                <div className="mt-auto pt-6 border-t border-dark-700">
                  {isCurrentPlan ? (
                    <button
                      disabled
                      className="w-full py-3 bg-dark-700 text-gray-400 rounded-lg cursor-not-allowed"
                    >
                      Current Plan
                    </button>
                  ) : canUpgrade ? (
                    <button
                      onClick={() => handleUpgrade(plan.tier)}
                      disabled={processingTier === plan.tier}
                      className="w-full btn-primary py-3 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {processingTier === plan.tier
                        ? 'Processing...'
                        : plan.priceGbpPerMonth > 0
                        ? 'Upgrade Now'
                        : 'Choose Plan'}
                    </button>
                  ) : (
                    <button
                      disabled
                      className="w-full py-3 bg-dark-700 text-gray-400 rounded-lg cursor-not-allowed"
                    >
                      Not Available
                    </button>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Free Trial Info */}
      {seller?.subscriptionTier === 'Free' && (
        <div className="card bg-gradient-to-r from-accent-900/20 to-primary-900/20 border-accent-500/30">
          <div className="flex items-start space-x-4">
            <div className="w-12 h-12 bg-accent-500/20 rounded-lg flex items-center justify-center flex-shrink-0">
              <svg
                className="w-6 h-6 text-accent-500"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            </div>
            <div className="flex-1">
              <h3 className="text-lg font-bold text-white">14-Day Free Trial</h3>
              <p className="text-gray-300 mt-2">
                All paid plans include a 14-day free trial. No credit card required to start.
                Cancel anytime during the trial period at no charge.
              </p>
            </div>
          </div>
        </div>
      )}

      {/* FAQ */}
      <div className="card">
        <h2 className="text-xl font-bold text-white mb-4">Frequently Asked Questions</h2>
        <div className="space-y-4">
          <div>
            <h3 className="text-white font-medium">Can I change my plan later?</h3>
            <p className="text-gray-400 text-sm mt-1">
              Yes, you can upgrade or downgrade your plan at any time. Changes are prorated
              automatically.
            </p>
          </div>
          <div>
            <h3 className="text-white font-medium">What happens if I exceed my listing limit?</h3>
            <p className="text-gray-400 text-sm mt-1">
              You'll be prompted to upgrade to a higher tier or remove existing listings to add new
              ones.
            </p>
          </div>
          <div>
            <h3 className="text-white font-medium">How does billing work?</h3>
            <p className="text-gray-400 text-sm mt-1">
              All plans are billed monthly. You'll receive an invoice via email after each billing
              cycle.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
