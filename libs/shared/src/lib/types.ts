/**
 * Shared types for the EV Marketplace application
 * These match the C# API models
 */

export interface ElectricVehicle {
  id: string;
  make: string;
  model: string;
  year: number;
  batteryCapacityKwh: number;
  wltpRangeKm: number;
  efficiencyKwhPer100Km: number;
  acChargeRateKw: number;
  dcChargeRateKw: number;
  connectorTypes: string[];
  bodyType: string;
  priceGbp: number;
}

export interface SearchCriteria {
  make?: string;
  bodyType?: string;
  minPrice?: number;
  maxPrice?: number;
  minRange?: number;
}

export interface TcoResult {
  purchasePriceGbp: number;
  totalEnergyCostGbp: number;
  totalCostGbp: number;
  years: number;
  annualMileageKm: number;
}

export interface RunningCostRequest {
  efficiencyKwhPer100Km: number;
  annualMileageKm: number;
  electricityCostPencePerKwh: number;
}

export interface RunningCostResponse {
  monthlyCostGbp: number;
}

export interface ChargingTimeRequest {
  batteryCapacityKwh: number;
  chargerPowerKw: number;
}

export interface ChargingTimeResponse {
  chargingTimeHours: number;
}

export interface TcoRequest {
  purchasePriceGbp: number;
  efficiencyKwhPer100Km: number;
  annualMileageKm: number;
  electricityCostPencePerKwh: number;
  years: number;
}

export interface RangeAnxietyRequest {
  wltpRangeKm: number;
  dailyCommuteKm: number;
}

export interface RangeAnxietyResponse {
  bufferPercentage: number;
}

// UI-specific types
export type BodyType = 'Sedan' | 'SUV' | 'Hatchback' | 'Estate' | 'Coupe' | 'MPV';

export type ConnectorType = 'Type 2' | 'CCS' | 'CHAdeMO';

export interface FilterOptions {
  makes: string[];
  bodyTypes: BodyType[];
  priceRange: {
    min: number;
    max: number;
  };
  rangeKm: {
    min: number;
    max: number;
  };
}

export interface ComparisonState {
  vehicles: ElectricVehicle[];
  maxVehicles: number;
}

// API Response wrappers
export interface ApiError {
  error: string;
}

export type ApiResponse<T> = T | ApiError;

export function isApiError(response: unknown): response is ApiError {
  return typeof response === 'object' && response !== null && 'error' in response;
}
