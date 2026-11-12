// src/services/signalrService.ts
import * as signalR from '@microsoft/signalr';
import type { Transaction } from '../types/transaction';

export type ConnectionStatus = 'Connected' | 'Reconnecting' | 'Disconnected';

class SignalRService {
  private connection: signalR.HubConnection | null = null;

  public async startConnection(
    onTransactionReceived: (transaction: Transaction) => void,
    onStatusChange: (status: ConnectionStatus) => void
  ): Promise<void> {
    // אם כבר קיים חיבור פעיל, אל תעשה כלום
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      onStatusChange('Connected');
      return;
    }

    if (this.connection) {
      await this.stopConnection();
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5000/hubs/transactions')
      .withAutomaticReconnect()
      .build();

    this.connection.on('ReceiveTransaction', (transaction: Transaction) => {
      onTransactionReceived(transaction);
    });

    this.connection.onreconnecting(() => {
      onStatusChange('Reconnecting');
    });

    this.connection.onreconnected(() => {
      onStatusChange('Connected');
    });

    this.connection.onclose(() => {
      onStatusChange('Disconnected');
    });

    const maxRetries = 5;
    const delayMs = 3000;

    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      // בדיקה מוקדמת: אם תוך כדי הריצות החיבור כבר הצליח, צא מהלופ מיד
      if (this.connection.state === signalR.HubConnectionState.Connected) {
        onStatusChange('Connected');
        return;
      }

      try {
        onStatusChange('Reconnecting');
        await this.connection.start();
        onStatusChange('Connected');
        console.log('SignalR Connected successfully! 🟢');
        return; // הצליח - יוצאים מהפונקציה בהצלחה
      } catch (error) {
        console.warn(`SignalR connection attempt ${attempt} failed. Retrying in ${delayMs / 1000}s...`);
        
        if (attempt === maxRetries) {
          onStatusChange('Disconnected');
          throw error;
        }
        await new Promise((resolve) => setTimeout(resolve, delayMs));
      }
    }
  }

  public async stopConnection(): Promise<void> {
    if (this.connection) {
      const connection = this.connection;
      this.connection = null;
      try {
        await connection.stop();
      } catch (err) {
        // התעלם משגיאות סגירה
      }
    }
  }
}

export const signalRService = new SignalRService();