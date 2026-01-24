import { Link } from 'react-router-dom';
import { useComparison } from '@ev-marketplace/shared';
import type { ElectricVehicle } from '@ev-marketplace/shared';

export function ComparePage() {
  const { vehicles, removeVehicle, clearVehicles, canAddMore } = useComparison(3);

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('en-GB', {
      style: 'currency',
      currency: 'GBP',
      maximumFractionDigits: 0,
    }).format(price);
  };

  // Empty state
  if (vehicles.length === 0) {
    return (
      <div className="min-h-screen bg-gradient-dark">
        <section className="section">
          <div className="container-custom">
            <div className="max-w-2xl mx-auto">
              <div className="card text-center py-16">
                <div className="mb-6">
                  <svg
                    className="w-24 h-24 mx-auto text-gray-600"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={1.5}
                      d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
                    />
                  </svg>
                </div>
                <h2 className="text-3xl font-display font-bold text-white mb-4">
                  No Vehicles to Compare
                </h2>
                <p className="text-gray-400 mb-8">
                  Browse our EV catalogue and add up to 3 vehicles to compare
                  their specs, pricing, and performance.
                </p>
                <Link to="/" className="btn-primary">
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
                      d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
                    />
                  </svg>
                  Browse EVs
                </Link>
              </div>
            </div>
          </div>
        </section>
      </div>
    );
  }

  const specs = [
    {
      label: 'Price',
      getValue: (v: ElectricVehicle) => formatPrice(v.priceGbp),
      compare: 'lower',
    },
    {
      label: 'WLTP Range',
      getValue: (v: ElectricVehicle) => `${Math.round(v.wltpRangeKm)} km`,
      compare: 'higher',
    },
    {
      label: 'Battery Capacity',
      getValue: (v: ElectricVehicle) => `${v.batteryCapacityKwh} kWh`,
      compare: 'higher',
    },
    {
      label: 'Efficiency',
      getValue: (v: ElectricVehicle) => `${v.efficiencyKwhPer100Km} kWh/100km`,
      compare: 'lower',
    },
    {
      label: 'DC Fast Charge',
      getValue: (v: ElectricVehicle) => `${v.dcChargeRateKw} kW`,
      compare: 'higher',
    },
    {
      label: 'AC Charge',
      getValue: (v: ElectricVehicle) => `${v.acChargeRateKw} kW`,
      compare: 'higher',
    },
    {
      label: 'Body Type',
      getValue: (v: ElectricVehicle) => v.bodyType,
      compare: 'none',
    },
    {
      label: 'Model Year',
      getValue: (v: ElectricVehicle) => v.year.toString(),
      compare: 'higher',
    },
  ];

  // Helper to determine if a value is best in category
  const isBest = (spec: typeof specs[0], vehicleIndex: number) => {
    if (spec.compare === 'none') return false;

    const values = vehicles.map((v) => {
      const val = spec.getValue(v);
      // Extract numeric value from string
      const match = val.match(/[\d,.]+/);
      return match ? parseFloat(match[0].replace(/,/g, '')) : 0;
    });

    const currentValue = values[vehicleIndex];

    if (spec.compare === 'higher') {
      return currentValue === Math.max(...values);
    } else {
      return currentValue === Math.min(...values);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-dark">
      {/* Header */}
      <section className="section bg-gradient-to-b from-dark-950 to-transparent">
        <div className="container-custom">
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 mb-8">
            <div>
              <h1 className="text-4xl md:text-5xl font-display font-bold text-white mb-2">
                Compare <span className="text-gradient">Electric Vehicles</span>
              </h1>
              <p className="text-gray-400">
                Comparing {vehicles.length} of 3 vehicles
              </p>
            </div>
            <div className="flex gap-3">
              {canAddMore && (
                <Link to="/" className="btn-secondary">
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
                      d="M12 6v6m0 0v6m0-6h6m-6 0H6"
                    />
                  </svg>
                  Add More
                </Link>
              )}
              <button onClick={clearVehicles} className="btn-secondary">
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
                Clear All
              </button>
            </div>
          </div>
        </div>
      </section>

      {/* Comparison Grid */}
      <section className="section">
        <div className="container-custom">
          {/* Vehicle Cards */}
          <div
            className={`grid gap-6 mb-8 ${
              vehicles.length === 1
                ? 'grid-cols-1 max-w-md mx-auto'
                : vehicles.length === 2
                ? 'grid-cols-1 md:grid-cols-2'
                : 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3'
            }`}
          >
            {vehicles.map((vehicle) => (
              <div key={vehicle.id} className="card-hover relative">
                <button
                  onClick={() => removeVehicle(vehicle.id)}
                  className="absolute top-4 right-4 w-8 h-8 bg-red-500/10 hover:bg-red-500/20 border border-red-500/20 hover:border-red-500 rounded-lg flex items-center justify-center text-red-400 transition-all"
                  title="Remove from comparison"
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
                      d="M6 18L18 6M6 6l12 12"
                    />
                  </svg>
                </button>

                <div className="mb-4">
                  <p className="text-sm text-gray-500 font-medium">
                    {vehicle.make}
                  </p>
                  <h3 className="text-2xl font-display font-semibold text-white">
                    {vehicle.model}
                  </h3>
                  <div className="flex gap-2 mt-2">
                    <span className="badge-gray">{vehicle.year}</span>
                    <span className="badge-primary">{vehicle.bodyType}</span>
                  </div>
                </div>

                <div className="mb-6">
                  <p className="text-3xl font-display font-bold text-gradient">
                    {formatPrice(vehicle.priceGbp)}
                  </p>
                </div>

                <Link
                  to={`/vehicle/${vehicle.id}`}
                  className="btn-secondary w-full"
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
                      d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
                    />
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
                    />
                  </svg>
                  View Full Details
                </Link>
              </div>
            ))}
          </div>

          {/* Comparison Table */}
          <div className="card overflow-hidden">
            <h2 className="text-2xl font-display font-semibold text-white mb-6">
              Detailed Comparison
            </h2>

            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-dark-800">
                    <th className="text-left py-4 pr-4 text-gray-400 font-medium">
                      Specification
                    </th>
                    {vehicles.map((vehicle) => (
                      <th
                        key={vehicle.id}
                        className="text-left py-4 px-4 text-white font-semibold"
                      >
                        <div className="min-w-[150px]">
                          <div className="text-sm text-gray-500">
                            {vehicle.make}
                          </div>
                          <div>{vehicle.model}</div>
                        </div>
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {specs.map((spec, idx) => (
                    <tr
                      key={spec.label}
                      className={
                        idx !== specs.length - 1 ? 'border-b border-dark-800' : ''
                      }
                    >
                      <td className="py-4 pr-4 text-gray-400 font-medium">
                        {spec.label}
                      </td>
                      {vehicles.map((vehicle, vIdx) => (
                        <td key={vehicle.id} className="py-4 px-4">
                          <span
                            className={`font-semibold ${
                              isBest(spec, vIdx)
                                ? 'text-accent-400'
                                : 'text-white'
                            }`}
                          >
                            {spec.getValue(vehicle)}
                          </span>
                          {isBest(spec, vIdx) && (
                            <svg
                              className="w-4 h-4 inline-block ml-2 text-accent-400"
                              fill="currentColor"
                              viewBox="0 0 20 20"
                            >
                              <path
                                fillRule="evenodd"
                                d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                                clipRule="evenodd"
                              />
                            </svg>
                          )}
                        </td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Charging Connectors */}
          <div className="card mt-6">
            <h2 className="text-2xl font-display font-semibold text-white mb-6">
              Charging Compatibility
            </h2>
            <div
              className={`grid gap-6 ${
                vehicles.length === 1
                  ? 'grid-cols-1'
                  : vehicles.length === 2
                  ? 'grid-cols-1 md:grid-cols-2'
                  : 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3'
              }`}
            >
              {vehicles.map((vehicle) => (
                <div key={vehicle.id} className="space-y-3">
                  <h3 className="font-semibold text-white">
                    {vehicle.make} {vehicle.model}
                  </h3>
                  <div className="flex flex-wrap gap-2">
                    {vehicle.connectorTypes.map((connector) => (
                      <span key={connector} className="badge-primary">
                        {connector}
                      </span>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Quick Calculations Info */}
          <div className="glass rounded-xl p-6 mt-6 text-center">
            <p className="text-gray-400">
              Want to see detailed cost calculations for each vehicle?
            </p>
            <p className="text-sm text-gray-500 mt-2">
              Click "View Full Details" on any vehicle above to access
              interactive calculators for running costs, charging time, and TCO.
            </p>
          </div>
        </div>
      </section>
    </div>
  );
}

export default ComparePage;
