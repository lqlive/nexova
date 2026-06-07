import type {
  DashboardItem,
  ChartItem,
  DatasetItem,
  DatasourcePreview,
  DataSourceItem,
} from './types';

const owner = (name: string): { name: string; initials: string } => ({
  name,
  initials: name
    .split(/[\s.]+/)
    .map((p) => p.charAt(0).toUpperCase())
    .slice(0, 2)
    .join(''),
});

export const dashboards: DashboardItem[] = [
  { id: 1, title: 'Sales Overview', status: 'published', owners: [owner('Zhang Wei'), owner('Li Ming')], modified: '2 hours ago', modifiedBy: 'Zhang Wei', favorite: true, charts: 12 },
  { id: 2, title: 'Marketing Funnel', status: 'published', owners: [owner('Wang Fang')], modified: '5 hours ago', modifiedBy: 'Wang Fang', favorite: false, charts: 8 },
  { id: 3, title: 'Product Engagement', status: 'draft', owners: [owner('Chen Qiang')], modified: 'yesterday', modifiedBy: 'Chen Qiang', favorite: true, charts: 15 },
  { id: 4, title: 'Financial KPIs', status: 'published', owners: [owner('Liu Jie'), owner('Zhang Wei')], modified: '2 days ago', modifiedBy: 'Liu Jie', favorite: false, charts: 6 },
  { id: 5, title: 'Customer Retention', status: 'published', owners: [owner('Sarah Kim')], modified: '3 days ago', modifiedBy: 'Sarah Kim', favorite: false, charts: 9 },
  { id: 6, title: 'Operations Monitor', status: 'draft', owners: [owner('Li Ming')], modified: '4 days ago', modifiedBy: 'Li Ming', favorite: true, charts: 11 },
  { id: 7, title: 'Website Traffic', status: 'published', owners: [owner('Wang Fang'), owner('Sarah Kim')], modified: '1 week ago', modifiedBy: 'Wang Fang', favorite: false, charts: 7 },
  { id: 8, title: 'Supply Chain Health', status: 'published', owners: [owner('Chen Qiang')], modified: '1 week ago', modifiedBy: 'Chen Qiang', favorite: false, charts: 5 },
];

export const charts: ChartItem[] = [
  { id: 1, name: 'Revenue by Region', vizType: 'Bar Chart', dataset: 'sales_fact', owners: [owner('Zhang Wei')], modified: '1 hour ago', modifiedBy: 'Zhang Wei', favorite: true },
  { id: 2, name: 'Daily Active Users', vizType: 'Line Chart', dataset: 'user_events', owners: [owner('Li Ming')], modified: '3 hours ago', modifiedBy: 'Li Ming', favorite: false },
  { id: 3, name: 'Conversion Funnel', vizType: 'Area Chart', dataset: 'marketing_funnel', owners: [owner('Wang Fang')], modified: '6 hours ago', modifiedBy: 'Wang Fang', favorite: true },
  { id: 4, name: 'Channel Distribution', vizType: 'Pie Chart', dataset: 'marketing_funnel', owners: [owner('Wang Fang')], modified: 'yesterday', modifiedBy: 'Wang Fang', favorite: false },
  { id: 5, name: 'Top Products', vizType: 'Table', dataset: 'sales_fact', owners: [owner('Chen Qiang')], modified: 'yesterday', modifiedBy: 'Chen Qiang', favorite: false },
  { id: 6, name: 'Total Revenue', vizType: 'Big Number', dataset: 'sales_fact', owners: [owner('Liu Jie')], modified: '2 days ago', modifiedBy: 'Liu Jie', favorite: true },
  { id: 7, name: 'Orders Over Time', vizType: 'Time-series', dataset: 'orders', owners: [owner('Zhang Wei')], modified: '3 days ago', modifiedBy: 'Zhang Wei', favorite: false },
  { id: 8, name: 'Activity Heatmap', vizType: 'Heatmap', dataset: 'user_events', owners: [owner('Sarah Kim')], modified: '4 days ago', modifiedBy: 'Sarah Kim', favorite: false },
  { id: 9, name: 'Global Sales Map', vizType: 'World Map', dataset: 'sales_fact', owners: [owner('Li Ming')], modified: '5 days ago', modifiedBy: 'Li Ming', favorite: false },
  { id: 10, name: 'Category Breakdown', vizType: 'Treemap', dataset: 'product_catalog', owners: [owner('Chen Qiang')], modified: '1 week ago', modifiedBy: 'Chen Qiang', favorite: true },
];

export const datasets: DatasetItem[] = [
  { id: 1, name: 'sales_fact', type: 'physical', database: 'PostgreSQL', schema: 'public', owners: [owner('Zhang Wei')], charts: 14, modified: '1 day ago' },
  { id: 2, name: 'user_events', type: 'physical', database: 'ClickHouse', schema: 'analytics', owners: [owner('Li Ming')], charts: 9, modified: '2 days ago' },
  { id: 3, name: 'marketing_funnel', type: 'virtual', database: 'PostgreSQL', schema: 'marketing', owners: [owner('Wang Fang')], charts: 6, modified: '3 days ago' },
  { id: 4, name: 'orders', type: 'physical', database: 'MySQL', schema: 'shop', owners: [owner('Chen Qiang')], charts: 7, modified: '4 days ago' },
  { id: 5, name: 'product_catalog', type: 'physical', database: 'MySQL', schema: 'shop', owners: [owner('Liu Jie')], charts: 4, modified: '5 days ago' },
  { id: 6, name: 'customer_360', type: 'virtual', database: 'Snowflake', schema: 'crm', owners: [owner('Sarah Kim')], charts: 11, modified: '1 week ago' },
];

export const overviewStats = [
  { label: 'Dashboards', value: dashboards.length, sub: 'total' },
  { label: 'Charts', value: charts.length, sub: 'total' },
  { label: 'Datasets', value: datasets.length, sub: 'total' },
  { label: 'Databases', value: 5, sub: 'connected' },
];

// ---------------------------------------------------------------------------
// Microsoft Fabric-style datasource detail (table preview)
// ---------------------------------------------------------------------------
const customers = [
  ['Chloe Garcia', 'chloe43'],
  ['Logan Collins', 'logan29'],
  ['Autumn Li', 'autumn12'],
  ['Cesar Sara', 'cesar9'],
  ['Peter She', 'peter8'],
  ['Jason Mitchell', 'jason10'],
  ['Nathaniel Cooper', 'nathan6'],
  ['Miguel Sanchez', 'miguel72'],
  ['Elijah Ross', 'elijah7'],
  ['Edward Taylor', 'edward1'],
  ['Maria Reed', 'maria4'],
  ['Ashlee Xu', 'ashlee12'],
  ['Melissa Richardson', 'melissa31'],
  ['Max Alvarez', 'max5'],
  ['Isabella Long', 'isabella6'],
  ['Miranda Ross', 'miranda4'],
  ['Kristi Mallrato', 'kristi2'],
  ['Billy Ortega', 'billy22'],
  ['Emily Flores', 'emily37'],
  ['Brenda Anan', 'brenda1'],
];

const items = [
  'Patch Kit/8 Patches',
  'Half-Finger Gloves, L',
  'All-Purpose Bike Stand',
  'Short-Sleeve Jersey, M',
  'Long-Sleeve Logo Jersey, XL',
  'Sport-100 Helmet, Black',
  'Sport-100 Helmet, Red',
  'Mountain Bottle Cage',
  'Water Bottle - 30 oz.',
  'Hydration Pack - 70 oz.',
  'Bike Wash - Dissolver',
  'Fender Set - Mountain',
  'Road Tire Tube',
];

const buildSalesRows = (count: number): (string | number)[][] => {
  const rows: (string | number)[][] = [];
  for (let i = 0; i < count; i++) {
    const cust = customers[i % customers.length];
    const item = items[i % items.length];
    const orderNo = `SO${51555 - i * 13}`;
    const lineNo = (i % 8) + 1;
    const day = ((i * 7) % 28) + 1;
    const month = ((i * 3) % 12) + 1;
    const date = `2021-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
    const qty = (i % 3) + 1;
    const price = [2.29, 24.49, 159.0, 53.99, 49.99, 34.99, 4.99, 9.99, 7.95][i % 9];
    const tax = +(price * qty * 0.08).toFixed(4);
    rows.push([orderNo, lineNo, date, cust[0], `${cust[1]}@adventure-works.com`, item, qty, price, tax]);
  }
  return rows;
};

export const datasourcePreview: DatasourcePreview = {
  columns: [
    { name: 'SalesOrderNumber', type: 'string' },
    { name: 'SalesOrderLineNumber', type: 'int' },
    { name: 'OrderDate', type: 'date' },
    { name: 'CustomerName', type: 'string' },
    { name: 'Email Address', type: 'string' },
    { name: 'Item', type: 'string' },
    { name: 'Quantity', type: 'int' },
    { name: 'UnitPrice', type: 'decimal' },
    { name: 'TaxAmount', type: 'decimal' },
  ],
  rows: buildSalesRows(40),
};

export const findDataset = (id: number) => datasets.find((d) => d.id === id);

export const datasourceTables = (database: string, current: string): string[] => {
  const siblings = datasets.filter((d) => d.database === database).map((d) => d.name);
  return Array.from(new Set([current, ...siblings]));
};

// ---------------------------------------------------------------------------
// Data sources (connections)
// ---------------------------------------------------------------------------
export const datasources: DataSourceItem[] = [
  { id: 1, name: 'Sales Warehouse', type: 'PostgreSQL', host: 'pg-sales.internal:5432', status: 'connected', datasets: 4, lastSync: '5 min ago', tables: ['sales_fact', 'customers', 'products', 'orders'] },
  { id: 2, name: 'Marketing DB', type: 'MySQL', host: 'mysql-mkt.internal:3306', status: 'connected', datasets: 3, lastSync: '12 min ago', tables: ['marketing_funnel', 'campaigns', 'leads'] },
  { id: 3, name: 'Events Store', type: 'ClickHouse', host: 'ch-events.internal:8123', status: 'syncing', datasets: 3, lastSync: 'syncing…', tables: ['user_events', 'sessions', 'pageviews'] },
  { id: 4, name: 'Finance Lake', type: 'Snowflake', host: 'xy12345.snowflakecomputing.com', status: 'connected', datasets: 5, lastSync: '1 hour ago', tables: ['revenue', 'costs', 'invoices', 'budgets'] },
  { id: 5, name: 'Product Analytics', type: 'BigQuery', host: 'bigquery.googleapis.com', status: 'error', datasets: 2, lastSync: 'failed 2h ago', tables: ['events', 'experiments'] },
  { id: 6, name: 'Ops Metrics', type: 'PostgreSQL', host: 'pg-ops.internal:5432', status: 'connected', datasets: 2, lastSync: '30 min ago', tables: ['metrics', 'alerts'] },
];

export const findDatasource = (id: number) => datasources.find((d) => d.id === id);
