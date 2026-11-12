import { useEffect, useMemo, useRef, useState } from 'react';
import { getTransactions } from '../services/apiService';
import { signalRService, type ConnectionStatus } from '../services/signalrService';
import type { Transaction, TransactionStatus } from '../types/transaction';
import { Icon } from '../components/Icon';

type TransactionFilter = 'All' | TransactionStatus;

const filters: TransactionFilter[] = ['All', 'Pending', 'Completed', 'Failed'];

const formatAmount = (amount: number) =>
  amount.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const formatTimestamp = (timestamp: string) =>
  new Date(timestamp).toLocaleString('en-US', {
    dateStyle: 'medium',
    timeStyle: 'short',
  });

export function MonitorPage() {
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus>('Disconnected');
  const [filterStatus, setFilterStatus] = useState<TransactionFilter>('All');
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [highlightedTransactionIds, setHighlightedTransactionIds] = useState<Set<string>>(
    () => new Set()
  );
  const highlightTimeouts = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map());

  useEffect(() => {
    let active = true;

    getTransactions()
      .then((data) => {
        if (active) setTransactions(data);
      })
      .catch((err: unknown) => {
        if (active) {
          setError(err instanceof Error ? err.message : 'Failed to load transactions.');
        }
      })
      .finally(() => {
        if (active) setIsLoading(false);
      });

    signalRService
      .startConnection(
        (newTransaction) => {
          if (!active) return;

          setTransactions((current) => [newTransaction, ...current]);
          setHighlightedTransactionIds((current) => new Set(current).add(newTransaction.transactionId));

          const previousTimeout = highlightTimeouts.current.get(newTransaction.transactionId);
          if (previousTimeout) clearTimeout(previousTimeout);

          const timeout = setTimeout(() => {
            highlightTimeouts.current.delete(newTransaction.transactionId);
            setHighlightedTransactionIds((current) => {
              const next = new Set(current);
              next.delete(newTransaction.transactionId);
              return next;
            });
          }, 1800);
          highlightTimeouts.current.set(newTransaction.transactionId, timeout);
        },
        (status) => {
          if (active) setConnectionStatus(status);
        }
      )
      .catch((err: unknown) => {
        console.error('Failed to start SignalR:', err);
      });

    return () => {
      active = false;
      highlightTimeouts.current.forEach(clearTimeout);
      highlightTimeouts.current.clear();
      void signalRService.stopConnection();
    };
  }, []);

  const stats = useMemo(() => {
    const amountByCurrency = transactions.reduce<Record<string, number>>((totals, transaction) => {
      totals[transaction.currency] = (totals[transaction.currency] ?? 0) + transaction.amount;
      return totals;
    }, {});

    return {
      total: transactions.length,
      completed: transactions.filter((transaction) => transaction.status === 'Completed').length,
      failed: transactions.filter((transaction) => transaction.status === 'Failed').length,
      amountByCurrency,
    };
  }, [transactions]);

  const filteredTransactions = useMemo(
    () =>
      filterStatus === 'All'
        ? transactions
        : transactions.filter((transaction) => transaction.status === filterStatus),
    [transactions, filterStatus]
  );

  const connectionLabel: Record<ConnectionStatus, string> = {
    Connected: 'Live connection',
    Reconnecting: 'Reconnecting',
    Disconnected: 'Disconnected',
  };

  return (
    <div className="page-container">
      <header className="page-header">
        <div>
          <h1 className="page-title">Transaction Monitor</h1>
          <p className="page-subtitle">Real-time financial transaction overview.</p>
        </div>
        <div
          className={`connection-indicator ${connectionStatus.toLowerCase()}`}
          role="status"
          aria-live="polite"
        >
          <span className="status-dot" />
          {connectionLabel[connectionStatus]}
        </div>
      </header>

      {error && (
        <div className="notice error" role="alert">
          <Icon className="notice-icon" name="x" size={16} />
          <span>Unable to load transactions: {error}</span>
        </div>
      )}

      <section className="summary-grid" aria-label="Transaction summary">
        <article className="summary-card">
          <div className="summary-card-top">
            <span className="summary-label">Total transactions</span>
            <span className="summary-icon blue"><Icon name="receipt" size={16} /></span>
          </div>
          <div className="summary-value">{stats.total.toLocaleString('en-US')}</div>
        </article>

        <article className="summary-card">
          <div className="summary-card-top">
            <span className="summary-label">Total volume</span>
            <span className="summary-icon"><Icon name="wallet" size={16} /></span>
          </div>
          <div className="summary-volume" aria-label="Total volume by currency">
            {Object.keys(stats.amountByCurrency).length === 0 ? (
              <span className="volume-empty">No volume yet</span>
            ) : (
              Object.entries(stats.amountByCurrency).map(([currency, total]) => (
                <div className="volume-item" key={currency} title={`${formatAmount(total)} ${currency}`}>
                  <span className="volume-amount truncate">{formatAmount(total)}</span>
                  <span className="volume-currency truncate">{currency}</span>
                </div>
              ))
            )}
          </div>
        </article>

        <article className="summary-card">
          <div className="summary-card-top">
            <span className="summary-label">Successful transactions</span>
            <span className="summary-icon green"><Icon name="check" size={17} /></span>
          </div>
          <div className="summary-value">{stats.completed.toLocaleString('en-US')}</div>
        </article>

        <article className="summary-card">
          <div className="summary-card-top">
            <span className="summary-label">Failed transactions</span>
            <span className="summary-icon red"><Icon name="x" size={16} /></span>
          </div>
          <div className="summary-value">{stats.failed.toLocaleString('en-US')}</div>
        </article>
      </section>

      <section className="panel" aria-labelledby="transactions-heading">
        <div className="panel-heading">
          <div>
            <h2 className="panel-title" id="transactions-heading">Transactions</h2>
            <p className="panel-description">Review and track activity across your account.</p>
          </div>
          <span className="transaction-count">
            {filteredTransactions.length.toLocaleString('en-US')} shown
          </span>
        </div>

        <div className="filter-bar" role="group" aria-label="Filter transactions by status">
          {filters.map((filter) => (
            <button
              aria-pressed={filterStatus === filter}
              className={`filter-button${filterStatus === filter ? ' active' : ''}`}
              key={filter}
              onClick={() => setFilterStatus(filter)}
              type="button"
            >
              {filter === 'All' ? 'All transactions' : filter}
            </button>
          ))}
        </div>

        <div className="table-scroll">
          <table className="transaction-table">
            <colgroup>
              <col className="column-id" />
              <col className="column-amount" />
              <col className="column-currency" />
              <col className="column-status" />
              <col className="column-timestamp" />
            </colgroup>
            <thead>
              <tr>
                <th scope="col">Transaction ID</th>
                <th scope="col">Amount</th>
                <th scope="col">Currency</th>
                <th scope="col">Status</th>
                <th scope="col">Timestamp</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td colSpan={5}>
                    <div className="loading-state">Loading transactions…</div>
                  </td>
                </tr>
              ) : filteredTransactions.length === 0 ? (
                <tr>
                  <td colSpan={5}>
                    <div className="empty-state">
                      <span className="empty-icon"><Icon name="receipt" size={20} /></span>
                      <p className="empty-title">
                        {filterStatus === 'All' ? 'No transactions yet' : `No ${filterStatus.toLowerCase()} transactions`}
                      </p>
                      <p className="empty-copy">
                        {filterStatus === 'All'
                          ? 'Transactions will appear here as soon as activity is received.'
                          : 'Try another status filter to see more activity.'}
                      </p>
                    </div>
                  </td>
                </tr>
              ) : (
                filteredTransactions.map((transaction) => (
                  <tr
                    className={
                      highlightedTransactionIds.has(transaction.transactionId)
                        ? 'new-transaction'
                        : undefined
                    }
                    key={transaction.transactionId}
                  >
                    <td className="id-cell">
                      <span className="transaction-id truncate" title={transaction.transactionId}>
                        {transaction.transactionId}
                      </span>
                    </td>
                    <td className="amount-cell">{formatAmount(transaction.amount)}</td>
                    <td className="currency-cell"><span className="currency-code">{transaction.currency}</span></td>
                    <td>
                      <span className={`status-badge ${transaction.status.toLowerCase()}`}>
                        {transaction.status}
                      </span>
                    </td>
                    <td className="timestamp-cell">
                      <time
                        className="timestamp truncate"
                        dateTime={transaction.timestamp}
                        title={formatTimestamp(transaction.timestamp)}
                      >
                        {formatTimestamp(transaction.timestamp)}
                      </time>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
