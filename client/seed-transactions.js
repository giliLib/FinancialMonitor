// seed-transactions.js (בשורש תיקיית ה־Client)
const API_URL = 'http://localhost:5000/api/Transactions';
async function generateTestTransactions(count = 10) {
  console.log(`🚀 מתחיל שליחת ${count} עסקאות בדיקה לשרת...`);

  const currencies = ['ILS', 'USD', 'EUR', 'GBP'];
  const statuses = ['Pending', 'Completed', 'Failed'];

  for (let i = 1; i <= count; i++) {
    const transaction = {
      transactionId: crypto.randomUUID(),
      amount: Number((Math.random() * 1000).toFixed(2)),
      currency: currencies[Math.floor(Math.random() * currencies.length)],
      status: statuses[Math.floor(Math.random() * statuses.length)],
      timestamp: new Date().toISOString()
    };

    try {
      const response = await fetch(API_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(transaction)
      });

      if (response.ok) {
        console.log(`✅ עסקה ${i}/${count} נוצרה בהצלחה: ${transaction.transactionId.substring(0, 8)}... (${transaction.amount} ${transaction.currency})`);
      } else {
        const errText = await response.text();
        console.error(`❌ שגיאה בעסקה ${i}:`, errText);
      }
    } catch (error) {
      console.error(`❌ שגיאת תקשורת בעסקה ${i}:`, error.message);
    }

    // השהיה של חצי שנייה בין עסקה לעסקה כדי לראות את הזמן אמת זורם ב־Monitor
    await new Promise(resolve => setTimeout(resolve, 500));
  }

  console.log('✨ סיום תהליך הזרקת העסקאות!');
}

generateTestTransactions(100);