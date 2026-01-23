import { useState } from 'react';
import { useChargingTimeCalculator } from '@ev-marketplace/shared';

interface ChargingTimeCalculatorProps {
  batteryCapacityKwh: number;
  dcChargeRateKw: number;
}

export function ChargingTimeCalculator({
  batteryCapacityKwh,
  dcChargeRateKw,
}: ChargingTimeCalculatorProps) {
  const [chargerPower, setChargerPower] = useState(dcChargeRateKw);
  const { result, loading, error, calculate } = useChargingTimeCalculator();

  const handleCalculate = () => {
    calculate({
      batteryCapacityKwh,
      chargerPowerKw: chargerPower,
    });
  };

  const chargerOptions = [
    { label: '7 kW (Home)', value: 7 },
    { label: '22 kW (Fast)', value: 22 },
    { label: '50 kW (Rapid)', value: 50 },
    { label: '150 kW (Ultra)', value: 150 },
    { label: `${dcChargeRateKw} kW (Max DC)`, value: dcChargeRateKw },
  ].sort((a, b) => a.value - b.value);

  return (
    <div className="card">
      <h3 className="text-xl font-display font-semibold text-white mb-4 flex items-center gap-2">
        <svg
          className="w-6 h-6 text-accent-400"
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
        Charging Time (10-80%)
      </h3>

      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-400 mb-2">
            Charger Type
          </label>
          <select
            value={chargerPower}
            onChange={(e) => setChargerPower(parseFloat(e.target.value))}
            className="input"
          >
            {chargerOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>

        <button onClick={handleCalculate} className="btn-accent w-full">
          Calculate
        </button>

        {loading && (
          <div className="text-center py-4">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-accent-500 mx-auto" />
          </div>
        )}

        {error && (
          <div className="bg-red-500/10 border border-red-500/20 rounded-lg p-4 text-red-400 text-sm">
            {error}
          </div>
        )}

        {result && !loading && (
          <div className="glass rounded-lg p-6 text-center">
            <p className="text-sm text-gray-400 mb-2">Charging Time</p>
            <p className="text-4xl font-display font-bold text-gradient">
              {result.chargingTimeHours < 1
                ? `${Math.round(result.chargingTimeHours * 60)} min`
                : `${result.chargingTimeHours.toFixed(1)} hrs`}
            </p>
            <p className="text-xs text-gray-500 mt-2">
              {batteryCapacityKwh * 0.7} kWh @ {chargerPower} kW
            </p>
          </div>
        )}
      </div>
    </div>
  );
}

export default ChargingTimeCalculator;
