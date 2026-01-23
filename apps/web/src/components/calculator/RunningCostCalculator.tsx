import { useState } from 'react';
import { useRunningCostCalculator } from '@ev-marketplace/shared';

interface RunningCostCalculatorProps {
  efficiencyKwhPer100Km: number;
}

export function RunningCostCalculator({
  efficiencyKwhPer100Km,
}: RunningCostCalculatorProps) {
  const [annualMileage, setAnnualMileage] = useState(10000);
  const [electricityCost, setElectricityCost] = useState(28);
  const { result, loading, error, calculate } = useRunningCostCalculator();

  const handleCalculate = () => {
    calculate({
      efficiencyKwhPer100Km,
      annualMileageKm: annualMileage,
      electricityCostPencePerKwh: electricityCost,
    });
  };

  return (
    <div className="card">
      <h3 className="text-xl font-display font-semibold text-white mb-4 flex items-center gap-2">
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
            d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
          />
        </svg>
        Monthly Running Cost
      </h3>

      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-400 mb-2">
            Annual Mileage: {annualMileage.toLocaleString()} km
          </label>
          <input
            type="range"
            min="5000"
            max="30000"
            step="1000"
            value={annualMileage}
            onChange={(e) => setAnnualMileage(parseInt(e.target.value))}
            className="w-full"
          />
          <div className="flex justify-between text-xs text-gray-500 mt-1">
            <span>5,000 km</span>
            <span>30,000 km</span>
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-400 mb-2">
            Electricity Cost: {electricityCost}p/kWh
          </label>
          <input
            type="range"
            min="10"
            max="50"
            step="1"
            value={electricityCost}
            onChange={(e) => setElectricityCost(parseInt(e.target.value))}
            className="w-full"
          />
          <div className="flex justify-between text-xs text-gray-500 mt-1">
            <span>10p/kWh</span>
            <span>50p/kWh</span>
          </div>
        </div>

        <button onClick={handleCalculate} className="btn-primary w-full">
          Calculate
        </button>

        {loading && (
          <div className="text-center py-4">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-500 mx-auto" />
          </div>
        )}

        {error && (
          <div className="bg-red-500/10 border border-red-500/20 rounded-lg p-4 text-red-400 text-sm">
            {error}
          </div>
        )}

        {result && !loading && (
          <div className="glass rounded-lg p-6 text-center">
            <p className="text-sm text-gray-400 mb-2">Monthly Cost</p>
            <p className="text-4xl font-display font-bold text-gradient">
              £{result.monthlyCostGbp.toFixed(2)}
            </p>
            <p className="text-xs text-gray-500 mt-2">
              Based on {(annualMileage / 12).toFixed(0)} km/month
            </p>
          </div>
        )}
      </div>
    </div>
  );
}

export default RunningCostCalculator;
