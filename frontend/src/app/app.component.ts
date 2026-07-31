import { Component, OnInit } from '@angular/core';
import jsPDF from 'jspdf';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  orders: any[] = [];
  aggregatedItems: { name: string; quantity: number }[] = [];
  period = 'today';
  private shop = '';
  private token = '';

  ngOnInit() {
    const params = new URLSearchParams(window.location.search);
    this.shop  = params.get('shop')  || '';
    this.token = params.get('token') || '';
    console.log('Shop:', this.shop);
    console.log('Token:', this.token ? this.token.substring(0, 15) + '...' : 'VUOTO');
    this.loadOrders();
  }

  loadOrders() {
    //const url = `http://localhost:5115/shopify/orders?period=${this.period}&shop=${this.shop}&token=${this.token}`;
    const url = `${environment.apiUrl}/shopify/orders?period=${this.period}&shop=${this.shop}&token=${this.token}`;

    fetch(url, { credentials: 'include' })
      .then(r => {
        if (!r.ok) throw new Error(`HTTP ${r.status}`);
        return r.json();
      })
      .then(data => {
        this.orders = data.orders || [];
        this.buildAggregated();
      })
      .catch(err => console.error('Errore chiamata API:', err));
  }

  changePeriod(p: string) {
    this.period = p;
    this.loadOrders();
  }

  buildAggregated() {
    const map = new Map<string, number>();
    for (const order of this.orders) {
      for (const item of order.line_items) {
        const key = item.variant_title
          ? `${item.title} - ${item.variant_title}`
          : item.title;
        map.set(key, (map.get(key) || 0) + item.quantity);
      }
    }
    this.aggregatedItems = Array.from(map.entries())
      .map(([name, quantity]) => ({ name, quantity }))
      .sort((a, b) => a.name.localeCompare(b.name));
  }

  downloadPdfConfezionamento() {
    const doc = new jsPDF();
    let y = 15;

    doc.setFontSize(16);
    doc.text('Lista Confezionamento', 10, y);
    y += 10;

    for (const order of this.orders) {
      doc.setFontSize(12);
      doc.setFont('helvetica', 'bold');
      doc.text(`${order.name} - ${order.customer?.first_name} ${order.customer?.last_name}`, 10, y);
      y += 6;

      doc.setFont('helvetica', 'normal');
      doc.setFontSize(10);
      doc.text(`${order.shipping_address?.address1}, ${order.shipping_address?.city} (${order.shipping_address?.zip})`, 10, y);
      y += 5;

      for (const item of order.line_items) {
        const label = item.variant_title
          ? `${item.title} - ${item.variant_title} x${item.quantity}`
          : `${item.title} x${item.quantity}`;
        doc.text(`  • ${label}`, 10, y);
        y += 5;
        if (y > 270) { doc.addPage(); y = 15; }
      }
      y += 4;
      doc.line(10, y, 200, y);
      y += 6;
      if (y > 270) { doc.addPage(); y = 15; }
    }

    doc.save(`confezionamento-${this.period}.pdf`);
  }

  downloadPdfTaglio() {
    const doc = new jsPDF();
    let y = 15;

    doc.setFontSize(16);
    doc.text('Lista Taglio Complessivo', 10, y);
    y += 10;

    doc.setFontSize(11);
    for (const item of this.aggregatedItems) {
      doc.text(`• ${item.name} × ${item.quantity}`, 10, y);
      y += 6;
      if (y > 270) { doc.addPage(); y = 15; }
    }

    doc.save(`taglio-${this.period}.pdf`);
  }
}
