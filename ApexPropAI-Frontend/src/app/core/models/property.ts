import { NearbyLocation } from "./nearbyLocation";

export interface Property {
  id: number;
  title: string;
  description: string;
  address: string;
  latitude: number;
  longitude: number;
  price: number;
  rooms: number;
  areaSqm: number;
  aiScore: number;
  estimatedValue?: number;
  aiAnalysisNotes?: string;
  images: string[];
  createdAt: Date;
  ownerId: number;
  nearbyLocations?: NearbyLocation[];
}

export interface PropertyPage {
  items: Property[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}