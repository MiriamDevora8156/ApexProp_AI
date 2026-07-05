import { NearbyLocation } from "./nearbyLocation";

export interface AIAnalysisResult {
  propertyId: number;
  propertyTitle: string;
  aiScore: number;
  scoreInterpretation: string;
  recommendation: string;
  analyzedAt: Date;
  nearbyLocations?: NearbyLocation[];
}

export interface PricePrediction {
  propertyId: number;
  currentPrice: number;
  predictedPrice: number;
  yearsAhead: number;
  growthFactor: number;
  percentageGrowth: number;
  priceDifference: number;
  predictedAt: Date;
}