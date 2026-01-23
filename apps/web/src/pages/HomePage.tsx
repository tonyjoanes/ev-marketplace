import { useState, useMemo } from 'react';
import { useVehicles, useComparison } from '@ev-marketplace/shared';
import { VehicleCard } from '../components/vehicle/VehicleCard';
import { LoadingSkeleton } from '../components/vehicle/LoadingSkeleton';
import { Link } from 'react-router-dom';

export function HomePage() {
  const { data: vehicles, loading, error } = useVehicles();
  const { vehicles: comparison, addVehicle, canAddMore } = useComparison(3);

  const [searchTerm, setSearchTerm] = useState('');
  const [selectedBodyType, setSelectedBodyType] = useState<string>('');
  const [priceRange, setPriceRange] = useState<[number, number]>([0, 100000]);

  // Get unique body types
  const bodyTypes = useMemo(() => {
    if (!vehicles) return [];
    const types = new Set(vehicles.map((v) => v.bodyType));
    return Array.from(types).sort();
  }, [vehicles]);

  // Filter vehicles
  const filteredVehicles = useMemo(() => {
    if (!vehicles) return [];

    return vehicles.filter((vehicle) => {
      const matchesSearch =
        vehicle.make.toLowerCase().includes(searchTerm.toLowerCase()) ||
        vehicle.model.toLowerCase().includes(searchTerm.toLowerCase());

      const matchesBodyType =
        !selectedBodyType || vehicle.bodyType === selectedBodyType;

      const matchesPrice =
        vehicle.priceGbp >= priceRange[0] && vehicle.priceGbp <= priceRange[1];

      return matchesSearch && matchesBodyType && matchesPrice;
    });
  }, [vehicles, searchTerm, selectedBodyType, priceRange]);

  const handleAddToComparison = (vehicle: typeof vehicles[0]) => {
    if (canAddMore) {
      addVehicle(vehicle);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-dark">
      {/* Hero Section */}
      <section className="section bg-gradient-to-b from-dark-950 to-transparent">
        <div className="container-custom">
          <div className="max-w-4xl mx-auto text-center space-y-6 animate-in">
            <h1 className="text-4xl md:text-6xl font-display font-bold">
              Find Your Perfect{' '}
              <span className="text-gradient">Electric Vehicle</span>
            </h1>
            <p className="text-xl text-gray-400">
              Browse, compare, and calculate costs for the UK's best electric
              vehicles
            </p>

            {/* Search Bar */}
            <div className="max-w-2xl mx-auto">
              <div className="relative">
                <svg
                  className="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-500"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
                  />
                </svg>
                <input
                  type="text"
                  placeholder="Search by make or model..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="input pl-12 pr-4 text-lg"
                />
              </div>
            </div>

            {/* Quick Stats */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 pt-8">
              <div className="glass rounded-xl p-4">
                <p className="text-3xl font-bold text-gradient">
                  {vehicles?.length || 0}
                </p>
                <p className="text-sm text-gray-400">EVs Available</p>
              </div>
              <div className="glass rounded-xl p-4">
                <p className="text-3xl font-bold text-gradient">
                  {bodyTypes.length}
                </p>
                <p className="text-sm text-gray-400">Body Types</p>
              </div>
              <div className="glass rounded-xl p-4">
                <p className="text-3xl font-bold text-gradient">
                  {comparison.length}/3
                </p>
                <p className="text-sm text-gray-400">In Comparison</p>
              </div>
              <div className="glass rounded-xl p-4">
                <Link to="/calculators" className="block group">
                  <p className="text-3xl font-bold text-gradient group-hover:scale-110 transition-transform">
                    4
                  </p>
                  <p className="text-sm text-gray-400">Calculators</p>
                </Link>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Filters and Results */}
      <section className="section">
        <div className="container-custom">
          {/* Filters */}
          <div className="flex flex-wrap gap-4 mb-8">
            <select
              value={selectedBodyType}
              onChange={(e) => setSelectedBodyType(e.target.value)}
              className="input w-auto"
            >
              <option value="">All Body Types</option>
              {bodyTypes.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>

            <div className="flex items-center gap-4 flex-1 max-w-md">
              <label className="text-sm text-gray-400 whitespace-nowrap">
                Price Range:
              </label>
              <input
                type="range"
                min="0"
                max="100000"
                step="5000"
                value={priceRange[1]}
                onChange={(e) =>
                  setPriceRange([0, parseInt(e.target.value)])
                }
                className="flex-1"
              />
              <span className="text-sm text-gray-400 whitespace-nowrap">
                £{(priceRange[1] / 1000).toFixed(0)}k
              </span>
            </div>

            {comparison.length > 0 && (
              <Link to="/compare" className="btn-accent ml-auto">
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
                    d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
                  />
                </svg>
                Compare {comparison.length} EVs
              </Link>
            )}
          </div>

          {/* Results Count */}
          <div className="mb-6">
            <p className="text-gray-400">
              Showing{' '}
              <span className="text-white font-semibold">
                {filteredVehicles.length}
              </span>{' '}
              {filteredVehicles.length === 1 ? 'vehicle' : 'vehicles'}
            </p>
          </div>

          {/* Error State */}
          {error && (
            <div className="card bg-red-500/10 border-red-500/20 text-red-400 text-center py-12">
              <svg
                className="w-12 h-12 mx-auto mb-4"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
              <p className="text-lg font-semibold mb-2">Failed to load vehicles</p>
              <p className="text-sm">{error}</p>
            </div>
          )}

          {/* Loading State */}
          {loading && (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {[...Array(6)].map((_, i) => (
                <LoadingSkeleton key={i} />
              ))}
            </div>
          )}

          {/* Vehicle Grid */}
          {!loading && !error && (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {filteredVehicles.map((vehicle) => (
                <VehicleCard
                  key={vehicle.id}
                  vehicle={vehicle}
                  onCompare={handleAddToComparison}
                  isInComparison={comparison.some((v) => v.id === vehicle.id)}
                />
              ))}
            </div>
          )}

          {/* No Results */}
          {!loading && !error && filteredVehicles.length === 0 && (
            <div className="card text-center py-12">
              <svg
                className="w-16 h-16 mx-auto mb-4 text-gray-600"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
              <p className="text-xl font-semibold text-gray-400 mb-2">
                No vehicles found
              </p>
              <p className="text-gray-500">
                Try adjusting your filters or search term
              </p>
            </div>
          )}
        </div>
      </section>
    </div>
  );
}

export default HomePage;
