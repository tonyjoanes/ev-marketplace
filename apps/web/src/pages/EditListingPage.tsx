import { useState, useEffect, FormEvent } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useAuth, useAuthenticatedFetch } from '@ev-marketplace/shared';
import { VehicleListing, ListingStatus } from './ListingsPage';

interface UpdateListingFormData {
  askingPriceGbp: number;
  description: string;
  location: string;
  mileage: number;
  status: ListingStatus;
}

export function EditListingPage() {
  const { id } = useParams<{ id: string }>();
  const { seller } = useAuth();
  const authenticatedFetch = useAuthenticatedFetch();
  const navigate = useNavigate();

  const [listing, setListing] = useState<VehicleListing | null>(null);
  const [formData, setFormData] = useState<UpdateListingFormData>({
    askingPriceGbp: 0,
    description: '',
    location: '',
    mileage: 0,
    status: ListingStatus.Active,
  });

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (id) {
      fetchListing();
    }
  }, [id]);

  const fetchListing = async () => {
    if (!id) return;

    try {
      setLoading(true);
      const response = await authenticatedFetch(`/api/listings/${id}`);

      if (!response.ok) {
        throw new Error('Failed to fetch listing');
      }

      const data: VehicleListing = await response.json();

      // Check if the seller owns this listing
      if (seller && data.sellerId !== seller.id) {
        setError('You do not have permission to edit this listing');
        return;
      }

      setListing(data);
      setFormData({
        askingPriceGbp: data.askingPriceGbp,
        description: data.description,
        location: data.location,
        mileage: data.mileage,
        status: data.status,
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load listing');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]:
        name === 'askingPriceGbp' || name === 'mileage' || name === 'status'
          ? Number(value)
          : value,
    }));
  };

  const validateForm = (): boolean => {
    if (formData.askingPriceGbp <= 0) {
      setError('Price must be positive');
      return false;
    }
    if (!formData.location.trim()) {
      setError('Location is required');
      return false;
    }
    if (!formData.description.trim() || formData.description.length < 20) {
      setError('Description must be at least 20 characters');
      return false;
    }
    if (formData.mileage < 0) {
      setError('Mileage cannot be negative');
      return false;
    }
    if (listing && formData.mileage < listing.mileage) {
      setError('Mileage cannot be decreased');
      return false;
    }
    return true;
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();

    if (!id || !seller) {
      setError('Invalid request');
      return;
    }

    if (!validateForm()) {
      return;
    }

    try {
      setSaving(true);
      setError('');

      const response = await authenticatedFetch(`/api/listings/${id}`, {
        method: 'PUT',
        body: JSON.stringify(formData),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Failed to update listing');
      }

      // Success - navigate to listings page
      navigate('/dashboard/listings');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update listing');
      console.error(err);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500 mx-auto"></div>
          <p className="mt-4 text-gray-400">Loading listing...</p>
        </div>
      </div>
    );
  }

  if (error && !listing) {
    return (
      <div className="card">
        <div className="bg-red-500/10 border border-red-500/50 rounded-lg p-4">
          <p className="text-red-400">{error}</p>
        </div>
        <button
          onClick={() => navigate('/dashboard/listings')}
          className="mt-4 btn bg-dark-800 hover:bg-dark-700 text-white"
        >
          Back to Listings
        </button>
      </div>
    );
  }

  if (!listing) {
    return (
      <div className="card">
        <p className="text-gray-400">Listing not found</p>
        <button
          onClick={() => navigate('/dashboard/listings')}
          className="mt-4 btn bg-dark-800 hover:bg-dark-700 text-white"
        >
          Back to Listings
        </button>
      </div>
    );
  }

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      {/* Header */}
      <div className="card">
        <h1 className="text-3xl font-bold text-white">Edit Listing</h1>
        <p className="text-gray-400 mt-2">
          {listing.year} {listing.make} {listing.model}
        </p>
      </div>

      {error && (
        <div className="bg-red-500/10 border border-red-500/50 rounded-lg p-4">
          <p className="text-red-400">{error}</p>
        </div>
      )}

      {/* Vehicle Details (Read-only) */}
      <div className="card">
        <h2 className="text-xl font-bold text-white mb-4">Vehicle Information</h2>
        <div className="grid grid-cols-2 md:grid-cols-3 gap-4 p-4 bg-dark-800 rounded-lg">
          <div>
            <p className="text-xs text-gray-500">Make & Model</p>
            <p className="text-sm font-semibold text-white">
              {listing.make} {listing.model}
            </p>
          </div>
          <div>
            <p className="text-xs text-gray-500">Year</p>
            <p className="text-sm font-semibold text-white">{listing.year}</p>
          </div>
          <div>
            <p className="text-xs text-gray-500">Body Type</p>
            <p className="text-sm font-semibold text-white">{listing.bodyType}</p>
          </div>
          <div>
            <p className="text-xs text-gray-500">Battery</p>
            <p className="text-sm font-semibold text-white">
              {listing.batteryCapacityKwh} kWh
            </p>
          </div>
          <div>
            <p className="text-xs text-gray-500">Range</p>
            <p className="text-sm font-semibold text-white">{listing.wltpRangeKm} km</p>
          </div>
          <div>
            <p className="text-xs text-gray-500">Efficiency</p>
            <p className="text-sm font-semibold text-white">
              {listing.efficiencyKwhPer100Km} kWh/100km
            </p>
          </div>
        </div>
        <p className="text-sm text-gray-500 mt-3">
          Vehicle specifications cannot be edited. Create a new listing to change these details.
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Editable Fields */}
        <div className="card">
          <h2 className="text-xl font-bold text-white mb-4">Listing Details</h2>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Asking Price (GBP) *
              </label>
              <input
                type="number"
                name="askingPriceGbp"
                value={formData.askingPriceGbp || ''}
                onChange={handleChange}
                min="0"
                step="100"
                className="w-full px-4 py-3 bg-dark-800 border border-dark-700 rounded-lg text-white focus:border-primary-500 focus:ring-1 focus:ring-primary-500"
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Mileage (miles) *
              </label>
              <input
                type="number"
                name="mileage"
                value={formData.mileage}
                onChange={handleChange}
                min={listing.mileage}
                step="1"
                className="w-full px-4 py-3 bg-dark-800 border border-dark-700 rounded-lg text-white focus:border-primary-500 focus:ring-1 focus:ring-primary-500"
                required
              />
              <p className="text-sm text-gray-500 mt-1">
                Current mileage: {listing.mileage.toLocaleString()} miles (can only increase)
              </p>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Location *
              </label>
              <input
                type="text"
                name="location"
                value={formData.location}
                onChange={handleChange}
                className="w-full px-4 py-3 bg-dark-800 border border-dark-700 rounded-lg text-white focus:border-primary-500 focus:ring-1 focus:ring-primary-500"
                placeholder="e.g., London, UK"
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Status *
              </label>
              <select
                name="status"
                value={formData.status}
                onChange={handleChange}
                className="w-full px-4 py-3 bg-dark-800 border border-dark-700 rounded-lg text-white focus:border-primary-500 focus:ring-1 focus:ring-primary-500"
                required
              >
                <option value={ListingStatus.Active}>Active</option>
                <option value={ListingStatus.Sold}>Sold</option>
                <option value={ListingStatus.Expired}>Expired</option>
                <option value={ListingStatus.Removed}>Removed</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Description *
              </label>
              <textarea
                name="description"
                value={formData.description}
                onChange={handleChange}
                rows={6}
                className="w-full px-4 py-3 bg-dark-800 border border-dark-700 rounded-lg text-white focus:border-primary-500 focus:ring-1 focus:ring-primary-500"
                placeholder="Describe your vehicle in detail (minimum 20 characters)..."
                required
              />
              <p className="text-sm text-gray-500 mt-2">
                {formData.description.length} / 20 characters minimum
              </p>
            </div>
          </div>
        </div>

        {/* Images (Read-only) */}
        {listing.imageUrls.length > 0 && (
          <div className="card">
            <h2 className="text-xl font-bold text-white mb-4">Images</h2>
            <div className="space-y-2">
              {listing.imageUrls.map((url, index) => (
                <div
                  key={index}
                  className="flex items-center gap-2 p-2 bg-dark-800 rounded-lg"
                >
                  <span className="flex-1 text-sm text-gray-300 truncate">{url}</span>
                </div>
              ))}
            </div>
            <p className="text-sm text-gray-500 mt-3">
              Image URLs cannot be edited. Create a new listing to change images.
            </p>
          </div>
        )}

        {/* Actions */}
        <div className="card">
          <div className="flex items-center gap-4">
            <button
              type="submit"
              disabled={saving}
              className="flex-1 btn-primary py-3 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {saving ? 'Saving Changes...' : 'Save Changes'}
            </button>
            <button
              type="button"
              onClick={() => navigate('/dashboard/listings')}
              disabled={saving}
              className="flex-1 btn bg-dark-800 hover:bg-dark-700 text-white py-3"
            >
              Cancel
            </button>
          </div>
        </div>
      </form>
    </div>
  );
}
