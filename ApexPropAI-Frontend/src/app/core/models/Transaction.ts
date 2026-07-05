export interface Transaction {
  address: string;
  price: number;
  date: Date;
  type: 'purchase' | 'lease';
}