// Shared BI domain types

export interface BiOwner {
  name: string;
  initials: string;
}

export interface DashboardItem {
  id: number;
  title: string;
  status: 'published' | 'draft';
  owners: BiOwner[];
  modified: string;
  modifiedBy: string;
  favorite: boolean;
  charts: number;
}

export type ChartVizType =
  | 'Bar Chart'
  | 'Line Chart'
  | 'Area Chart'
  | 'Pie Chart'
  | 'Table'
  | 'Big Number'
  | 'Time-series'
  | 'Heatmap'
  | 'World Map'
  | 'Treemap';

export interface ChartItem {
  id: number;
  name: string;
  vizType: ChartVizType;
  dataset: string;
  owners: BiOwner[];
  modified: string;
  modifiedBy: string;
  favorite: boolean;
}

export interface DatasetItem {
  id: number;
  name: string;
  type: 'physical' | 'virtual';
  database: string;
  schema: string;
  owners: BiOwner[];
  charts: number;
  modified: string;
}

export type GridColumnType = 'string' | 'int' | 'decimal' | 'date';

export interface GridColumn {
  name: string;
  type: GridColumnType;
}

export interface DatasourcePreview {
  columns: GridColumn[];
  rows: (string | number)[][];
}

export interface DataSourceItem {
  id: number;
  name: string;
  type: string;
  host: string;
  status: 'connected' | 'error' | 'syncing';
  datasets: number;
  lastSync: string;
  tables: string[];
}
