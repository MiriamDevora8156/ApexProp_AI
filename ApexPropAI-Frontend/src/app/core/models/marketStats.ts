import { Transaction } from './Transaction';

export interface MarketStats {
  sparklineData: number[]; // 12 months price trend
  heatIndex: number; // 0-100 demand index
  recentTransactions: Transaction[];
  avgPrice: number;
  priceChange: string; // "+5.2%" or "-2.1%"
  areaDescription: string;
}