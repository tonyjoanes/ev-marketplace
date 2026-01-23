import { useParams, Link, useNavigate } from 'react-router-dom';
import { useVehicle, useComparison } from '@ev-marketplace/shared';
import { RunningCostCalculator } from '../components/calculator/RunningCostCalculator';
import { ChargingTimeCalculator } from '../components/calculator/ChargingTimeCalculator';
import { TcoCalculator } from '../components/calculator/TcoCalculator';

export function VehicleDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: vehicle, loading, error } = useVehicle(id);
  const { vehicles: comparison, addVehicle, canAddMore } = useComparison(3);

  const isInComparison = vehicle
    ? comparison.some((v) => v.id === vehicle.id)
    : false;

  const handleAddToComparison = () => {
    if (vehicle && canAddMore) {
      addVehicle(vehicle);
    }
  };

  if (loading) {
    return (
      <div className="section container-custom">
        <div className="animate-pulse space-y-8">
          <div className="h-12 w-64 bg-dark-700 rounded" />
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
            <div className="lg:col-span-2 space-y-6">
              <div className="h-64 bg-dark-700 rounded-xl" />
              <div className="h-96 bg-dark-700 rounded-xl" />
            </div>
            <div className="space-y-6">
              <div className="h-64 bg-dark-700 rounded-xl" />
              <div className="h-64 bg-dark-700 rounded-xl" />
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (error || !vehicle) {
    return (
      <div className="section container-custom">
        <div className="card bg-red-500/10 border-red-500/20 text-red-400 text-center py-12">
          <svg
            className="w-16 h-16 mx-auto mb-4"
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
          <p className="text-xl font-semibold mb-4">Vehicle Not Found</p>
          <p className="text-sm mb-6">{error || 'This vehicle does not exist'}</p>
          <Link to="/" className="btn-primary">
            Back to Browse
          </Link>
        </div>
      </div>
    );
  }

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('en-GB', {
      style: 'currency',
      currency: 'GBP',
      maximumFractionDigits: 0,
    }).format(price);
  };

  return (
    <div className="min-h-screen bg-gradient-dark">
      {/* Header Section */}
      <section className="section bg-gradient-to-b from-dark-950 to-transparent">
        <div className="container-custom">
          {/* Breadcrumb */}
          <nav className="flex items-center gap-2 text-sm text-gray-400 mb-6">
            <Link to="/" className="hover:text-primary-400 transition-colors">
              Browse
            </Link>
            <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z"
                clipRule="evenodd"
              />
            </svg>
            <span className="text-white">{vehicle.make}</span>
            <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z"
                clipRule="evenodd"
              />
            </svg>
            <span className="text-white">{vehicle.model}</span>
          </nav>

          {/* Hero */}
          <div className="flex flex-col lg:flex-row gap-8 items-start">
            <div className="flex-1">
              <div className="flex items-start gap-4 mb-4">
                <div>
                  <p className="text-sm text-gray-500 font-medium">
                    {vehicle.make}
                  </p>
                  <h1 className="text-4xl md:text-5xl font-display font-bold text-white mb-2">
                    {vehicle.model}
                  </h1>
                  <div className="flex flex-wrap gap-2">
                    <span className="badge-primary">{vehicle.year}</span>
                    <span className="badge-gray">{vehicle.bodyType}</span>
                  </div>
                </div>
              </div>

              <p className="text-xl text-gray-400 mb-6">
                {vehicle.batteryCapacityKwh} kWh battery • {vehicle.wltpRangeKm}{' '}
                km WLTP range
              </p>

              <div className="flex flex-wrap gap-3">
                <button
                  onClick={() => navigate(-1)}
                  className="btn-secondary"
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
                      d="M10 19l-7-7m0 0l7-7m-7 7h18"
                    />
                  </svg>
                  Back
                </button>
                <button
                  onClick={handleAddToComparison}
                  disabled={!canAddMore && !isInComparison}
                  className={`btn-secondary ${
                    isInComparison ? 'bg-accent-500/20 border-accent-500' : ''
                  }`}
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
                      d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
                    />
                  </svg>
                  {isInComparison ? 'In Comparison' : 'Add to Compare'}
                </button>
                {comparison.length > 0 && (
                  <Link to="/compare" className="btn-accent">
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
                    Compare {comparison.length}
                  </Link>
                )}
              </div>
            </div>

            {/* Price Card */}
            <div className="glass rounded-xl p-6 lg:min-w-[300px]">
              <p className="text-sm text-gray-400 mb-2">Starting Price</p>
              <p className="text-4xl font-display font-bold text-gradient mb-4">
                {formatPrice(vehicle.priceGbp)}
              </p>
              <button className="btn-primary w-full">
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
                    d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z"
                  />
                </svg>
                Find Dealers
              </button>
            </div>
          </div>
        </div>
      </section>

      {/* Main Content */}
      <section className="section">
        <div className="container-custom">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
            {/* Left Column - Specs */}
            <div className="lg:col-span-2 space-y-6">
              {/* Key Specifications */}
              <div className="card">
                <h2 className="text-2xl font-display font-semibold text-white mb-6">
                  Key Specifications
                </h2>
                <div className="grid grid-cols-2 md:grid-cols-3 gap-6">
                  <div className="space-y-2">
                    <div className="flex items-center gap-2 text-gray-400">
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
                          d="M13 10V3L4 14h7v7l9-11h-7z"
                        />
                      </svg>
                      <span className="text-sm">Battery</span>
                    </div>
                    <p className="text-2xl font-bold text-white">
                      {vehicle.batteryCapacityKwh} kWh
                    </p>
                  </div>

                  <div className="space-y-2">
                    <div className="flex items-center gap-2 text-gray-400">
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
                          d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7"
                        />
                      </svg>
                      <span className="text-sm">WLTP Range</span>
                    </div>
                    <p className="text-2xl font-bold text-accent-400">
                      {Math.round(vehicle.wltpRangeKm)} km
                    </p>
                  </div>

                  <div className="space-y-2">
                    <div className="flex items-center gap-2 text-gray-400">
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
                          d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6"
                        />
                      </svg>
                      <span className="text-sm">Efficiency</span>
                    </div>
                    <p className="text-2xl font-bold text-white">
                      {vehicle.efficiencyKwhPer100Km}
                    </p>
                    <p className="text-xs text-gray-500">kWh/100km</p>
                  </div>

                  <div className="space-y-2">
                    <div className="flex items-center gap-2 text-gray-400">
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
                          d="M13 10V3L4 14h7v7l9-11h-7z"
                        />
                      </svg>
                      <span className="text-sm">AC Charge</span>
                    </div>
                    <p className="text-2xl font-bold text-white">
                      {vehicle.acChargeRateKw} kW
                    </p>
                  </div>

                  <div className="space-y-2">
                    <div className="flex items-center gap-2 text-gray-400">
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
                          d="M13 10V3L4 14h7v7l9-11h-7z"
                        />
                      </svg>
                      <span className="text-sm">DC Fast Charge</span>
                    </div>
                    <p className="text-2xl font-bold text-primary-400">
                      {vehicle.dcChargeRateKw} kW
                    </p>
                  </div>

                  <div className="space-y-2">
                    <div className="flex items-center gap-2 text-gray-400">
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
                          d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
                        />
                      </svg>
                      <span className="text-sm">Model Year</span>
                    </div>
                    <p className="text-2xl font-bold text-white">
                      {vehicle.year}
                    </p>
                  </div>
                </div>
              </div>

              {/* Connectors */}
              <div className="card">
                <h2 className="text-2xl font-display font-semibold text-white mb-4">
                  Charging Connectors
                </h2>
                <div className="flex flex-wrap gap-3">
                  {vehicle.connectorTypes.map((connector) => (
                    <div
                      key={connector}
                      className="glass rounded-lg px-4 py-3 flex items-center gap-3"
                    >
                      <svg
                        className="w-6 h-6 text-primary-400"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M13 10V3L4 14h7v7l9-11h-7z"
                        />
                      </svg>
                      <span className="font-medium">{connector}</span>
                    </div>
                  ))}
                </div>
              </div>
            </div>

            {/* Right Column - Calculators */}
            <div className="space-y-6">
              <RunningCostCalculator
                efficiencyKwhPer100Km={vehicle.efficiencyKwhPer100Km}
              />
              <ChargingTimeCalculator
                batteryCapacityKwh={vehicle.batteryCapacityKwh}
                dcChargeRateKw={vehicle.dcChargeRateKw}
              />
              <TcoCalculator
                purchasePriceGbp={vehicle.priceGbp}
                efficiencyKwhPer100Km={vehicle.efficiencyKwhPer100Km}
              />
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}

export default VehicleDetailPage;
