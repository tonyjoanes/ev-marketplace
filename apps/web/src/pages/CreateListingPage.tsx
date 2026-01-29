import { useState, FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth, useAuthenticatedFetch } from '@ev-marketplace/shared';
import { VehicleCondition } from './ListingsPage';

interface CreateListingFormData {
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
  catalogueVehicleId?: string;
}

export function CreateListingPage() {
  const { seller } = useAuth();
  const authenticatedFetch = useAuthenticatedFetch();
  const navigate = useNavigate();

  const [formData, setFormData] = useState<CreateListingFormData>({
    make: '',
    model: '',
    year: new Date().getFullYear(),
    batteryCapacityKwh: 0,
    wltpRangeKm: 0,
    efficiencyKwhPer100Km: 0,
    bodyType: 'SUV',
    condition: VehicleCondition.Good,
    mileage: 0,
    askingPriceGbp: 0,
    location: '',
    description: '',
    imageUrls: [],
  });

  const [imageUrlInput, setImageUrlInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]:
        name === 'year' ||
        name === 'condition' ||
        name === 'batteryCapacityKwh' ||
        name === 'wltpRangeKm' ||
        name === 'efficiencyKwhPer100Km' ||
        name === 'mileage' ||
        name === 'askingPriceGbp'
          ? Number(value)
          : value,
    }));
  };

  const handleAddImageUrl = () => {
    if (imageUrlInput.trim()) {
      setFormData((prev) => ({
        ...prev,
        imageUrls: [...prev.imageUrls, imageUrlInput.trim()],
      }));
      setImageUrlInput('');
    }
  };

  const handleRemoveImageUrl = (index: number) => {
    setFormData((prev) => ({
      ...prev,
      imageUrls: prev.imageUrls.filter((_, i) => i !== index),
    }));
  };

  const validateForm = (): boolean => {
    if (!formData.make.trim()) {
      setError('Make is required');
      return false;
    }
    if (!formData.model.trim()) {
      setError('Model is required');
      return false;
    }
    if (formData.year < 2010 || formData.year > new Date().getFullYear() + 1) {
      setError('Year must be between 2010 and next year');
      return false;
    }
    if (formData.batteryCapacityKwh <= 0) {
      setError('Battery capacity must be positive');
      return false;
    }
    if (formData.wltpRangeKm <= 0) {
      setError('Range must be positive');
      return false;
    }
    if (formData.efficiencyKwhPer100Km <= 0) {
      setError('Efficiency must be positive');
      return false;
    }
    if (formData.mileage < 0) {
      setError('Mileage cannot be negative');
      return false;
    }
    if (formData.condition === VehicleCondition.New && formData.mileage > 100) {
      setError('New vehicles should have minimal mileage (max 100 miles)');
      return false;
    }
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
    return true;
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();

    if (!seller) {
      setError('You must be logged in to create a listing');
      return;
    }

    if (!validateForm()) {
      return;
    }

    try {
      setLoading(true);
      setError('');

      const response = await authenticatedFetch('/api/listings', {
        method: 'POST',
        body: JSON.stringify({
          sellerId: seller.id,
          ...formData,
        }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Failed to create listing');
      }

      // Success - navigate to listings page
      navigate('/dashboard/listings');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create listing');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const bodyTypes = ['Sedan', 'SUV', 'Hatchback', 'Coupe', 'Van', 'Truck', 'Convertible', 'Wagon'];

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      {/* Header */}
      <div className="card">
        <h1 className="text-3xl font-bold text-white">Create New Listing</h1>
        <p className="text-gray-400 mt-2">
          Fill in the details below to list your electric vehicle
        </p>
      </div>

      {error && (
        <div className="bg-red-500/10 border border-red-500/50 rounded-lg p-4">
          <p className="text-red-400">{error}</p>
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Vehicle Information */}
        <div className="card">
          <h2 className="text-xl font-bold text-white mb-4">Vehicle Information</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Make *
              </label>
              <input
                type="text"
                name="make"
                value={formData.make}
                onChange={handleChange}
                className="w-full px-4 py-3 bg-dark-800 border border-dark-700 rounded-lg text-white focus:border-primary-500 focus:ring-1 focus:ring-primary-500"
                placeholder="e.g., Tesla"
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Model *
              </label>
              <input
                type="text"
                name="model"
                value={formData.model}
                onChange={handleChange}
                className="w-full px-4 py-3 bg-dark-800 border border-dark-700 rounded-lg text-white focus:border-primary-500 focus:ring-1 focus:ring-primary-500"
                placeholder="e.g., Model 3"
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Year *
              </label>
              <input
                type="number"
                name="year"
                value={formData.year}
                onChange={handleChange}
                min="2010"
                max={new Date().getFullYear() + 1}
                className="w-full px-4 py-3 bg-dark-800 border border-dark-700 rounded-lg text-white focus:border-primary-500 focus:ring-1 focus:ring-primary-500"
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Body Type *
              </label>
              <select
                name="bodyType"
                value={formData.bodyType}
                onChange={handleChange}
                className="w-full px-4 py-3 bg-dark-800 border border-dark-700 rounded-lg text-white focus:border-primary-500 focus:ring-1 focus:ring-primary-500"
                required
              >
                {bodyTypes.map((type) => (
                  <option key={type} value={type}>
                    {type}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Condition *
              </label>
              <select
                name="condition"
                value={formData.condition}
                onChange={handleChange}
                className="w-full px-4 py-3 bg-dark-800 border border-dark-700 rounded-lg text-white focus:border-primary-500 focus:ring-1 focus:ring-primary-500"
                required
              >
                <option value={VehicleCondition.New}>New</option>
                <option value={VehicleCondition.Excellent}>Excellent</option>
                <option value={VehicleCondition.Good}>Good</option>
                <option value={VehicleCondition.Fair}>Fair</option>
              </select>
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
                min="0"
                step="1"
                className="w-full px-4 py-3 bg-dark-800 border border-dark-700 rounded-lg text-white focus:border-primary-500 focus:ring-1 focus:ring-primary-500"
                required
              />
            </div>
          </div>
        </div>

        {/* Battery & Performance */}
        <div className="card">
          <h2 className="text-xl font-bold text-white mb-4">Battery & Performance</h2>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Battery Capacity (kWh) *
              </label>
              <input
                type="number"
                name="batteryCapacityKwh"
                value={formData.batteryCapacityKwh || ''}
                onChange={handleChange}
                min="0"
                step="0.1"
                className="w-full px-4 py-3 bg-dark-800 border border-dark-700 rounded-lg text-white focus:border-primary-500 focus:ring-1 focus:ring-primary-500"
                placeholder="e.g., 75"
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                WLTP Range (km) *
              </label>
              <input
                type="number"
                name="wltpRangeKm"
                value={formData.wltpRangeKm || ''}
                onChange={handleChange}
                min="0"
                step="1"
                className="w-full px-4 py-3 bg-dark-800 border border-dark-700 rounded-lg text-white focus:border-primary-500 focus:ring-1 focus:ring-primary-500"
                placeholder="e.g., 450"
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Efficiency (kWh/100km) *
              </label>
              <input
                type="number"
                name="efficiencyKwhPer100Km"
                value={formData.efficiencyKwhPer100Km || ''}
                onChange={handleChange}
                min="0"
                step="0.1"
                className="w-full px-4 py-3 bg-dark-800 border border-dark-700 rounded-lg text-white focus:border-primary-500 focus:ring-1 focus:ring-primary-500"
                placeholder="e.g., 16.7"
                required
              />
            </div>
          </div>
        </div>

        {/* Pricing & Location */}
        <div className="card">
          <h2 className="text-xl font-bold text-white mb-4">Pricing & Location</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
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
                placeholder="e.g., 35000"
                required
              />
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
          </div>
        </div>

        {/* Description */}
        <div className="card">
          <h2 className="text-xl font-bold text-white mb-4">Description</h2>
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

        {/* Image URLs */}
        <div className="card">
          <h2 className="text-xl font-bold text-white mb-4">Images</h2>
          <div className="space-y-4">
            <div className="flex gap-2">
              <input
                type="url"
                value={imageUrlInput}
                onChange={(e) => setImageUrlInput(e.target.value)}
                placeholder="Enter image URL and click Add"
                className="flex-1 px-4 py-3 bg-dark-800 border border-dark-700 rounded-lg text-white focus:border-primary-500 focus:ring-1 focus:ring-primary-500"
              />
              <button
                type="button"
                onClick={handleAddImageUrl}
                className="btn bg-dark-800 hover:bg-dark-700 text-white"
              >
                Add
              </button>
            </div>

            {formData.imageUrls.length > 0 && (
              <div className="space-y-2">
                {formData.imageUrls.map((url, index) => (
                  <div
                    key={index}
                    className="flex items-center gap-2 p-2 bg-dark-800 rounded-lg"
                  >
                    <span className="flex-1 text-sm text-gray-300 truncate">{url}</span>
                    <button
                      type="button"
                      onClick={() => handleRemoveImageUrl(index)}
                      className="text-red-400 hover:text-red-300"
                    >
                      <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Actions */}
        <div className="card">
          <div className="flex items-center gap-4">
            <button
              type="submit"
              disabled={loading}
              className="flex-1 btn-primary py-3 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {loading ? 'Creating Listing...' : 'Create Listing'}
            </button>
            <button
              type="button"
              onClick={() => navigate('/dashboard/listings')}
              disabled={loading}
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
