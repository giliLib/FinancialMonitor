
import type { CreateTransactionRequest, Transaction } from '../types/transaction';

const API_BASE_URL = 'http://localhost:5000/api/Transactions';

export const createTransaction = async (
  data: CreateTransactionRequest
): Promise<Transaction> => {
  const response = await fetch(API_BASE_URL, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(errorText || 'Failed to create transaction');
  }

  return response.json();
};

export const getTransactions = async (): Promise<Transaction[]> => {
  const response = await fetch(API_BASE_URL);

  if (!response.ok) {
    throw new Error('Failed to fetch transactions');
  }

  return response.json();
};
