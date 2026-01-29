import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth, useAuthenticatedFetch } from '@ev-marketplace/shared';

export interface VehicleListing {
  id: string;
  sellerId: string;
  make: string;
  model: string;
  year: number;
  batteryCapacityKwh: number;
  wltpRangeKm: number;
  efficiencyKwhPer100Km: number;
  bodyType: string;
  condition: VehicleCondition;
  mileage: number;
  askingPriceGbp: number;
  location: string;
  description: string;
  imageUrls: string[];
  status: ListingStatus;
  listedAt: string;
  soldAt?: string;
  catalogueVehicleId?: string;
}

export enum VehicleCondition {
  New = 0,
  Excellent = 1,
  Good = 2,
  Fair = 3,
}

export enum ListingStatus {
  Active = 0,
  Sold = 1,
  Expired = 2,
  Removed = 3,
}

export function ListingsPage() {
  const { seller } = useAuth();
  const authenticatedFetch = useAuthenticatedFetch();
  const navigate = useNavigate();

  const [listings, setListings] = useState<VehicleListing[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [deletingId, setDeletingId] = useState<string | null>(null);

  useEffect(() => {
    if (seller) {
      fetchListings();
    }
  }, [seller]);

  const fetchListings = async () => {
    if (!seller) return;

    try {
      setLoading(true);
      const response = await authenticatedFetch(`/api/listings/seller/${seller.id}`);

      if (!response.ok) {
        throw new Error('Failed to fetch listings');
      }

      const data = await response.json();
      setListings(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load listings');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this listing?')) {
      return;
    }

    try {
      setDeletingId(id);
      const response = await authenticatedFetch(`/api/listings/${id}`, {
        method: 'DELETE',
      });

      if (!response.ok) {
        throw new Error('Failed to delete listing');
      }

      // Remove from local state
      setListings(listings.filter((l) => l.id !== id));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete listing');
    } finally {
      setDeletingId(null);
    }
  };

  const handleMarkAsSold = async (id: string) => {
    if (!confirm('Mark this listing as sold?')) {
      return;
    }

    try {
      const response = await authenticatedFetch(`/api/listings/${id}/mark-sold`, {
        method: 'POST',
      });

      if (!response.ok) {
        throw new Error('Failed to mark as sold');
      }

      // Refresh listings
      await fetchListings();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update listing');
    }
  };

  const canCreateListing = () => {
    if (!seller) return false;
    return seller.remainingListings > 0 || seller.remainingListings === Number.MAX_SAFE_INTEGER;
  };

  const getStatusBadge = (status: ListingStatus) => {
    const badges = {
      [ListingStatus.Active]: 'bg-green-500/20 text-green-400 border-green-500/50',
      [ListingStatus.Sold]: 'bg-blue-500/20 text-blue-400 border-blue-500/50',
      [ListingStatus.Expired]: 'bg-yellow-500/20 text-yellow-400 border-yellow-500/50',
      [ListingStatus.Removed]: 'bg-gray-500/20 text-gray-400 border-gray-500/50',
    };

    const labels = {
      [ListingStatus.Active]: 'Active',
      [ListingStatus.Sold]: 'Sold',
      [ListingStatus.Expired]: 'Expired',
      [ListingStatus.Removed]: 'Removed',
    };

    return (
      <span className={`px-2 py-1 text-xs font-medium rounded-md border ${badges[status]}`}>
        {labels[status]}
      </span>
    );
  };

  const getConditionLabel = (condition: VehicleCondition) => {
    const labels = {
      [VehicleCondition.New]: 'New',
      [VehicleCondition.Excellent]: 'Excellent',
      [VehicleCondition.Good]: 'Good',
      [VehicleCondition.Fair]: 'Fair',
    };
    return labels[condition];
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500 mx-auto"></div>
          <p className="mt-4 text-gray-400">Loading your listings...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-white">My Listings</h1>
          <p className="text-gray-400 mt-2">
            {listings.length} listing{listings.length !== 1 ? 's' : ''} •{' '}
            {seller?.remainingListings === Number.MAX_SAFE_INTEGER
              ? 'Unlimited remaining'
              : `${seller?.remainingListings} remaining`}
          </p>
        </div>

        {canCreateListing() ? (
          <Link to="/dashboard/listings/new" className="btn-primary">
            <svg
              className="w-5 h-5"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 4v16m8-8H4"
              />
            </svg>
            Create Listing
          </Link>
        ) : (
          <div className="text-right">
            <button disabled className="btn-primary opacity-50 cursor-not-allowed">
              <svg
                className="w-5 h-5"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 4v16m8-8H4"
                />
              </svg>
              Create Listing
            </button>
            <p className="text-sm text-yellow-400 mt-2">
              Listing limit reached.{' '}
              <Link to="/dashboard/subscription" className="underline hover:text-yellow-300">
                Upgrade your plan
              </Link>
            </p>
          </div>
        )}
      </div>

      {error && (
        <div className="bg-red-500/10 border border-red-500/50 rounded-lg p-4">
          <p className="text-red-400">{error}</p>
        </div>
      )}

      {listings.length === 0 ? (
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
                d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
              />
            </svg>
          </div>
          <h2 className="text-xl font-bold text-white mb-2">No listings yet</h2>
          <p className="text-gray-400 mb-6">Create your first listing to start selling</p>
          {canCreateListing() && (
            <Link to="/dashboard/listings/new" className="btn-primary inline-flex">
              Create Your First Listing
            </Link>
          )}
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {listings.map((listing) => (
            <div key={listing.id} className="card hover:border-primary-500/50 transition-all">
              {/* Listing Header */}
              <div className="flex items-start justify-between mb-4">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-2">
                    <h3 className="text-xl font-bold text-white">
                      {listing.year} {listing.make} {listing.model}
                    </h3>
                    {getStatusBadge(listing.status)}
                  </div>
                  <p className="text-sm text-gray-400">
                    {getConditionLabel(listing.condition)} • {listing.mileage.toLocaleString()} miles
                  </p>
                </div>
              </div>

              {/* Vehicle Details */}
              <div className="grid grid-cols-2 gap-4 py-4 border-y border-dark-700">
                <div>
                  <p className="text-xs text-gray-500">Battery</p>
                  <p className="text-sm font-semibold text-white">
                    {listing.batteryCapacityKwh} kWh
                  </p>
                </div>
                <div>
                  <p className="text-xs text-gray-500">Range</p>
                  <p className="text-sm font-semibold text-white">
                    {listing.wltpRangeKm} km
                  </p>
                </div>
                <div>
                  <p className="text-xs text-gray-500">Location</p>
                  <p className="text-sm font-semibold text-white truncate">
                    {listing.location}
                  </p>
                </div>
                <div>
                  <p className="text-xs text-gray-500">Listed</p>
                  <p className="text-sm font-semibold text-white">
                    {new Date(listing.listedAt).toLocaleDateString()}
                  </p>
                </div>
              </div>

              {/* Price */}
              <div className="py-4">
                <p className="text-3xl font-bold text-gradient">
                  £{listing.askingPriceGbp.toLocaleString()}
                </p>
              </div>

              {/* Actions */}
              <div className="flex items-center gap-2">
                {listing.status === ListingStatus.Active && (
                  <>
                    <Link
                      to={`/dashboard/listings/edit/${listing.id}`}
                      className="flex-1 btn bg-dark-800 hover:bg-dark-700 text-white text-center"
                    >
                      Edit
                    </Link>
                    <button
                      onClick={() => handleMarkAsSold(listing.id)}
                      className="flex-1 btn bg-green-600 hover:bg-green-700 text-white"
                    >
                      Mark as Sold
                    </button>
                  </>
                )}

                {listing.status === ListingStatus.Sold && (
                  <div className="flex-1 text-center py-2 bg-blue-500/10 text-blue-400 rounded-lg text-sm">
                    Sold on {listing.soldAt ? new Date(listing.soldAt).toLocaleDateString() : 'N/A'}
                  </div>
                )}

                <button
                  onClick={() => handleDelete(listing.id)}
                  disabled={deletingId === listing.id}
                  className="btn bg-red-600 hover:bg-red-700 text-white disabled:opacity-50 disabled:cursor-not-allowed"
                  title="Delete listing"
                >
                  <svg
                    className="w-5 h-5"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                    />
                  </svg>
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
