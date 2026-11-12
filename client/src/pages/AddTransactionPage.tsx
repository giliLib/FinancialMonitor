import { useState, type FormEvent } from 'react';
import { createTransaction } from '../services/apiService';
import type { TransactionStatus } from '../types/transaction';
import { Icon } from '../components/Icon';

interface FormErrors {
  transactionId?: string;
  amount?: string;
  currency?: string;
  general?: string;
}

function getErrorMessage(error: unknown): string {
  if (error instanceof Response) {
    return error.statusText || 'Unable to create transaction. Please try again.';
  }

  if (!(error instanceof Error)) {
    return 'Unable to create transaction. Please try again.';
  }

  const jsonStartIndex = error.message.indexOf('{');
  if (jsonStartIndex === -1) return error.message;

  try {
    const response = JSON.parse(error.message.substring(jsonStartIndex)) as {
      message?: string;
      title?: string;
    };
    return response.message || response.title || error.message;
  } catch {
    return error.message;
  }
}

export function AddTransactionPage() {
  const [transactionId, setTransactionId] = useState<string>(crypto.randomUUID());
  const [amount, setAmount] = useState<number | string>(100);
  const [currency, setCurrency] = useState('ILS');
  const [status, setStatus] = useState<TransactionStatus>('Pending');
  const [errors, setErrors] = useState<FormErrors>({});
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const validate = (): boolean => {
    const newErrors: FormErrors = {};

    if (!transactionId.trim()) {
      newErrors.transactionId = 'Transaction ID is required.';
    }

    const numericAmount = Number(amount);
    if (amount === '' || !Number.isFinite(numericAmount) || numericAmount <= 0) {
      newErrors.amount = 'Enter a valid amount greater than 0.';
    }

    if (!/^[A-Z]{3}$/.test(currency)) {
      newErrors.currency = 'Use a 3-letter currency code, such as ILS or USD.';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSuccessMessage(null);

    if (!validate()) return;

    setIsLoading(true);
    try {
      await createTransaction({
        transactionId,
        amount: Number(amount),
        currency,
        status,
        timestamp: new Date().toISOString(),
      });

      setSuccessMessage('Transaction created successfully and sent to the live monitor.');
      setErrors({});
      setTransactionId(crypto.randomUUID());
    } catch (error: unknown) {
      const errorMessage = getErrorMessage(error);
      if (errorMessage.includes('already exists')) {
        setErrors({ transactionId: 'A transaction with this ID already exists. Enter a unique ID.' });
      } else {
        setErrors({ general: errorMessage });
      }
    } finally {
      setIsLoading(false);
    }
  };

  const clearError = (field: keyof FormErrors) => {
    setErrors((current) => ({ ...current, [field]: undefined }));
    setSuccessMessage(null);
  };

  return (
    <div className="page-container">
      <header className="page-header">
        <div>
          <h1 className="page-title">Add transaction</h1>
          <p className="page-subtitle">Create and publish a transaction to your monitor.</p>
        </div>
      </header>

      <div className="form-layout">
        {successMessage && (
          <div className="notice success" role="status">
            <Icon className="notice-icon" name="check" size={16} />
            <span>{successMessage}</span>
          </div>
        )}

        {errors.general && (
          <div className="notice error" role="alert">
            <Icon className="notice-icon" name="x" size={16} />
            <span>{errors.general}</span>
          </div>
        )}

        <section className="form-card" aria-labelledby="create-transaction-heading">
          <div className="form-card-heading">
            <h2 className="form-card-title" id="create-transaction-heading">Transaction details</h2>
            <p className="form-card-description">
              Enter the transaction information below. It will be available in the live monitor after creation.
            </p>
          </div>

          <form className="transaction-form" onSubmit={handleSubmit} noValidate>
            <div className="form-field">
              <label className="form-label" htmlFor="transaction-id">Transaction ID</label>
              <input
                aria-describedby={errors.transactionId ? 'transaction-id-error' : 'transaction-id-hint'}
                aria-invalid={Boolean(errors.transactionId)}
                autoComplete="off"
                className="form-input mono"
                id="transaction-id"
                onChange={(event) => {
                  setTransactionId(event.target.value);
                  clearError('transactionId');
                }}
                placeholder="Enter a unique transaction ID"
                type="text"
                value={transactionId}
              />
              {errors.transactionId ? (
                <p className="field-error" id="transaction-id-error">{errors.transactionId}</p>
              ) : (
                <p className="form-hint" id="transaction-id-hint">A unique identifier for this transaction.</p>
              )}
            </div>

            <div className="form-row">
              <div className="form-field">
                <label className="form-label" htmlFor="amount">Amount</label>
                <input
                  aria-describedby={errors.amount ? 'amount-error' : undefined}
                  aria-invalid={Boolean(errors.amount)}
                  className="form-input"
                  id="amount"
                  min="0.01"
                  onChange={(event) => {
                    setAmount(event.target.value);
                    clearError('amount');
                  }}
                  placeholder="0.00"
                  step="0.01"
                  type="number"
                  value={amount}
                />
                {errors.amount && <p className="field-error" id="amount-error">{errors.amount}</p>}
              </div>

              <div className="form-field">
                <label className="form-label" htmlFor="currency">Currency</label>
                <input
                  aria-describedby={errors.currency ? 'currency-error' : 'currency-hint'}
                  aria-invalid={Boolean(errors.currency)}
                  autoComplete="off"
                  className="form-input"
                  id="currency"
                  maxLength={3}
                  onChange={(event) => {
                    setCurrency(event.target.value.toUpperCase());
                    clearError('currency');
                  }}
                  placeholder="ILS"
                  type="text"
                  value={currency}
                />
                {errors.currency ? (
                  <p className="field-error" id="currency-error">{errors.currency}</p>
                ) : (
                  <p className="form-hint" id="currency-hint">3-letter code</p>
                )}
              </div>
            </div>

            <div className="form-field">
              <label className="form-label" htmlFor="status">Initial status</label>
              <select
                className="form-select"
                id="status"
                onChange={(event) => setStatus(event.target.value as TransactionStatus)}
                value={status}
              >
                <option value="Pending">Pending</option>
                <option value="Completed">Completed</option>
                <option value="Failed">Failed</option>
              </select>
            </div>

            <button className="primary-button" disabled={isLoading} type="submit">
              {isLoading ? (
                <>
                  <span className="button-spinner" aria-hidden="true" />
                  Creating transaction…
                </>
              ) : (
                <>
                  <Icon name="plus" size={16} />
                  Create transaction
                </>
              )}
            </button>
          </form>
        </section>
      </div>
    </div>
  );
}
