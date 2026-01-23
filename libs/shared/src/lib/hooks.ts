/**
 * Custom React hooks for API data fetching
 */

import { useState, useEffect, useCallback } from 'react';
import { apiClient } from './api-client';
import type {
  ElectricVehicle,
  SearchCriteria,
  RunningCostRequest,
  RunningCostResponse,
  ChargingTimeRequest,
  ChargingTimeResponse,
  TcoRequest,
  TcoResult,
} from './types';

interface UseAsyncState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
}

export function useVehicles(): UseAsyncState<ElectricVehicle[]> {
  const [data, setData] = useState<ElectricVehicle[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const vehicles = await apiClient.getAllVehicles();
      setData(vehicles);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return { data, loading, error, refetch: fetchData };
}

export function useVehicle(id: string | undefined): UseAsyncState<ElectricVehicle> {
  const [data, setData] = useState<ElectricVehicle | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    if (!id) {
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      setError(null);
      const vehicle = await apiClient.getVehicleById(id);
      setData(vehicle);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return { data, loading, error, refetch: fetchData };
}

export function useVehicleSearch() {
  const [data, setData] = useState<ElectricVehicle[] | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const search = useCallback(async (criteria: SearchCriteria) => {
    try {
      setLoading(true);
      setError(null);
      const vehicles = await apiClient.searchVehicles(criteria);
      setData(vehicles);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }, []);

  return { data, loading, error, search };
}

export function useRunningCostCalculator() {
  const [result, setResult] = useState<RunningCostResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const calculate = useCallback(async (request: RunningCostRequest) => {
    try {
      setLoading(true);
      setError(null);
      const response = await apiClient.calculateRunningCost(request);
      setResult(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Calculation failed');
    } finally {
      setLoading(false);
    }
  }, []);

  return { result, loading, error, calculate };
}

export function useChargingTimeCalculator() {
  const [result, setResult] = useState<ChargingTimeResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const calculate = useCallback(async (request: ChargingTimeRequest) => {
    try {
      setLoading(true);
      setError(null);
      const response = await apiClient.calculateChargingTime(request);
      setResult(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Calculation failed');
    } finally {
      setLoading(false);
    }
  }, []);

  return { result, loading, error, calculate };
}

export function useTcoCalculator() {
  const [result, setResult] = useState<TcoResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const calculate = useCallback(async (request: TcoRequest) => {
    try {
      setLoading(true);
      setError(null);
      const response = await apiClient.calculateTco(request);
      setResult(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Calculation failed');
    } finally {
      setLoading(false);
    }
  }, []);

  return { result, loading, error, calculate };
}

// Utility hook for managing comparison state
export function useComparison(maxVehicles: number = 3) {
  const [vehicles, setVehicles] = useState<ElectricVehicle[]>([]);

  const addVehicle = useCallback(
    (vehicle: ElectricVehicle) => {
      setVehicles((prev) => {
        if (prev.find((v) => v.id === vehicle.id)) {
          return prev; // Already in comparison
        }
        if (prev.length >= maxVehicles) {
          return [...prev.slice(1), vehicle]; // Replace oldest
        }
        return [...prev, vehicle];
      });
    },
    [maxVehicles]
  );

  const removeVehicle = useCallback((id: string) => {
    setVehicles((prev) => prev.filter((v) => v.id !== id));
  }, []);

  const clearVehicles = useCallback(() => {
    setVehicles([]);
  }, []);

  const canAddMore = vehicles.length < maxVehicles;

  return {
    vehicles,
    addVehicle,
    removeVehicle,
    clearVehicles,
    canAddMore,
  };
}
