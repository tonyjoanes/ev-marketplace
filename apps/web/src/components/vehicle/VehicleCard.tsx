import { Link } from 'react-router-dom';
import type { ElectricVehicle } from '@ev-marketplace/shared';

interface VehicleCardProps {
  vehicle: ElectricVehicle;
  onCompare?: (vehicle: ElectricVehicle) => void;
  isInComparison?: boolean;
}

export function VehicleCard({
  vehicle,
  onCompare,
  isInComparison = false,
}: VehicleCardProps) {
  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('en-GB', {
      style: 'currency',
      currency: 'GBP',
      maximumFractionDigits: 0,
    }).format(price);
  };

  return (
    <div className="card-hover group">
      {/* Header with Make and Model */}
      <div className="flex items-start justify-between mb-4">
        <div>
          <p className="text-sm text-gray-500 font-medium">{vehicle.make}</p>
          <h3 className="text-xl font-display font-semibold text-white group-hover:text-gradient transition-all">
            {vehicle.model}
          </h3>
        </div>
        <span className="badge-gray">{vehicle.year}</span>
      </div>

      {/* Key Stats Grid */}
      <div className="grid grid-cols-2 gap-4 mb-6">
        <div className="space-y-1">
          <p className="text-xs text-gray-500 uppercase tracking-wider">Range</p>
          <p className="text-lg font-semibold text-accent-400">
            {Math.round(vehicle.wltpRangeKm)} km
          </p>
        </div>
        <div className="space-y-1">
          <p className="text-xs text-gray-500 uppercase tracking-wider">Battery</p>
          <p className="text-lg font-semibold text-primary-400">
            {vehicle.batteryCapacityKwh} kWh
          </p>
        </div>
        <div className="space-y-1">
          <p className="text-xs text-gray-500 uppercase tracking-wider">
            Efficiency
          </p>
          <p className="text-sm font-medium text-gray-300">
            {vehicle.efficiencyKwhPer100Km} kWh/100km
          </p>
        </div>
        <div className="space-y-1">
          <p className="text-xs text-gray-500 uppercase tracking-wider">
            DC Charge
          </p>
          <p className="text-sm font-medium text-gray-300">
            {vehicle.dcChargeRateKw} kW
          </p>
        </div>
      </div>

      {/* Body Type and Connectors */}
      <div className="flex flex-wrap gap-2 mb-6">
        <span className="badge-primary">{vehicle.bodyType}</span>
        {vehicle.connectorTypes.slice(0, 2).map((connector) => (
          <span key={connector} className="badge-gray text-xs">
            {connector}
          </span>
        ))}
      </div>

      {/* Price */}
      <div className="mb-6">
        <p className="text-2xl font-display font-bold text-white">
          {formatPrice(vehicle.priceGbp)}
        </p>
        <p className="text-sm text-gray-500">Starting price</p>
      </div>

      {/* Actions */}
      <div className="flex gap-2">
        <Link to={`/vehicle/${vehicle.id}`} className="btn-primary flex-1">
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
          View Details
        </Link>
        {onCompare && (
          <button
            onClick={(e) => {
              e.preventDefault();
              onCompare(vehicle);
            }}
            className={`btn-secondary ${
              isInComparison ? 'bg-accent-500/20 border-accent-500' : ''
            }`}
            title={isInComparison ? 'In comparison' : 'Add to comparison'}
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
          </button>
        )}
      </div>
    </div>
  );
}

export default VehicleCard;
