import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Layout from './components/Layout/Layout';
import Home from './pages/Home';
import Dashboards from './pages/Dashboards';
import Charts from './pages/Charts';
import Datasets from './pages/Datasets';
import Datasources from './pages/Datasources';
import DatasourceDetail from './pages/DatasourceDetail';
import SqlEditor from './pages/SqlEditor';
import Login from './pages/Login';
import Register from './pages/Register';
import { AuthProvider } from './auth/AuthContext';
import { RequireAuth, RequireNoAuth } from './auth/guards';

const AppContent: React.FC = () => (
  <Router>
    <Routes>
      <Route
        path="/login"
        element={
          <RequireNoAuth>
            <Login />
          </RequireNoAuth>
        }
      />
      <Route
        path="/register"
        element={
          <RequireNoAuth>
            <Register />
          </RequireNoAuth>
        }
      />

      <Route
        path="/*"
        element={
          <RequireAuth>
            <Layout>
              <Routes>
                <Route path="/" element={<Home />} />
                <Route path="/dashboards" element={<Dashboards />} />
                <Route path="/charts" element={<Charts />} />
                <Route path="/datasets" element={<Datasets />} />
                <Route path="/datasources" element={<Datasources />} />
                <Route path="/datasources/:id" element={<DatasourceDetail />} />
                <Route path="/sql-editor" element={<SqlEditor />} />
                <Route path="/sql-editor/:id" element={<SqlEditor />} />
              </Routes>
            </Layout>
          </RequireAuth>
        }
      />
    </Routes>
  </Router>
);

const App: React.FC = () => (
  <AuthProvider>
    <AppContent />
  </AuthProvider>
);

export default App;
