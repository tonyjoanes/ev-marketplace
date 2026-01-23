/**
 * API Client for EV Marketplace backend
 * Functional approach with proper error handling
 */

import type {
  ElectricVehicle,
  SearchCriteria,
  RunningCostRequest,
  RunningCostResponse,
  ChargingTimeRequest,
  ChargingTimeResponse,
  TcoRequest,
  TcoResult,
  RangeAnxietyRequest,
  RangeAnxietyResponse,
  ApiError,
} from './types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

class ApiClient {
  private baseUrl: string;

  constructor(baseUrl: string = API_BASE_URL) {
    this.baseUrl = baseUrl;
  }

  private async request<T>(
    endpoint: string,
    options?: RequestInit
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;

    const response = await fetch(url, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options?.headers,
      },
    });

    if (!response.ok) {
      const error: ApiError = await response.json().catch(() => ({
        error: `HTTP ${response.status}: ${response.statusText}`,
      }));
      throw new Error(error.error || 'An error occurred');
    }

    return response.json();
  }

  // Vehicle endpoints
  async getAllVehicles(): Promise<ElectricVehicle[]> {
    return this.request<ElectricVehicle[]>('/api/vehicles');
  }

  async getVehicleById(id: string): Promise<ElectricVehicle> {
    return this.request<ElectricVehicle>(`/api/vehicles/${id}`);
  }

  async searchVehicles(criteria: SearchCriteria): Promise<ElectricVehicle[]> {
    return this.request<ElectricVehicle[]>('/api/vehicles/search', {
      method: 'POST',
      body: JSON.stringify(criteria),
    });
  }

  // Calculator endpoints
  async calculateRunningCost(
    request: RunningCostRequest
  ): Promise<RunningCostResponse> {
    return this.request<RunningCostResponse>(
      '/api/calculators/running-cost',
      {
        method: 'POST',
        body: JSON.stringify(request),
      }
    );
  }

  async calculateChargingTime(
    request: ChargingTimeRequest
  ): Promise<ChargingTimeResponse> {
    return this.request<ChargingTimeResponse>(
      '/api/calculators/charging-time',
      {
        method: 'POST',
        body: JSON.stringify(request),
      }
    );
  }

  async calculateTco(request: TcoRequest): Promise<TcoResult> {
    return this.request<TcoResult>('/api/calculators/tco', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async calculateRangeAnxiety(
    request: RangeAnxietyRequest
  ): Promise<RangeAnxietyResponse> {
    return this.request<RangeAnxietyResponse>(
      '/api/calculators/range-anxiety',
      {
        method: 'POST',
        body: JSON.stringify(request),
      }
    );
  }
}

// Export singleton instance
export const apiClient = new ApiClient();
export default apiClient;
