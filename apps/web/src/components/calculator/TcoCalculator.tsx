import { useState } from 'react';
import { useTcoCalculator } from '@ev-marketplace/shared';

interface TcoCalculatorProps {
  purchasePriceGbp: number;
  efficiencyKwhPer100Km: number;
}

export function TcoCalculator({
  purchasePriceGbp,
  efficiencyKwhPer100Km,
}: TcoCalculatorProps) {
  const [annualMileage, setAnnualMileage] = useState(10000);
  const [electricityCost, setElectricityCost] = useState(28);
  const [years, setYears] = useState(5);
  const { result, loading, error, calculate } = useTcoCalculator();

  const handleCalculate = () => {
    calculate({
      purchasePriceGbp,
      efficiencyKwhPer100Km,
      annualMileageKm: annualMileage,
      electricityCostPencePerKwh: electricityCost,
      years,
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
            d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z"
          />
        </svg>
        Total Cost of Ownership
      </h3>

      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-400 mb-2">
            Ownership Period: {years} years
          </label>
          <input
            type="range"
            min="1"
            max="10"
            step="1"
            value={years}
            onChange={(e) => setYears(parseInt(e.target.value))}
            className="w-full"
          />
          <div className="flex justify-between text-xs text-gray-500 mt-1">
            <span>1 year</span>
            <span>10 years</span>
          </div>
        </div>

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
        </div>

        <button onClick={handleCalculate} className="btn-primary w-full">
          Calculate TCO
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
          <div className="space-y-3">
            <div className="glass rounded-lg p-6 text-center">
              <p className="text-sm text-gray-400 mb-2">
                Total Cost ({result.years} years)
              </p>
              <p className="text-4xl font-display font-bold text-gradient">
                £{result.totalCostGbp.toLocaleString()}
              </p>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="glass rounded-lg p-4 text-center">
                <p className="text-xs text-gray-400 mb-1">Purchase Price</p>
                <p className="text-xl font-semibold text-white">
                  £{result.purchasePriceGbp.toLocaleString()}
                </p>
              </div>
              <div className="glass rounded-lg p-4 text-center">
                <p className="text-xs text-gray-400 mb-1">Energy Costs</p>
                <p className="text-xl font-semibold text-white">
                  £{result.totalEnergyCostGbp.toLocaleString()}
                </p>
              </div>
            </div>

            <p className="text-xs text-gray-500 text-center">
              Based on {result.annualMileageKm.toLocaleString()} km/year for{' '}
              {result.years} years
            </p>
          </div>
        )}
      </div>
    </div>
  );
}

export default TcoCalculator;
