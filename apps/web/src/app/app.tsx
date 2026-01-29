import { Route, Routes } from 'react-router-dom';
import { AuthProvider } from '@ev-marketplace/shared';
import { Layout } from '../components/layout';
import { HomePage } from '../pages/HomePage';
import { VehicleDetailPage } from '../pages/VehicleDetailPage';
import { ComparePage } from '../pages/ComparePage';
import { LoginPage } from '../pages/LoginPage';
import { RegisterPage } from '../pages/RegisterPage';
import { DashboardLayout, DashboardOverviewPage } from '../pages/DashboardPage';
import { SubscriptionPage } from '../pages/SubscriptionPage';
import { ListingsPage } from '../pages/ListingsPage';
import { CreateListingPage } from '../pages/CreateListingPage';
import { EditListingPage } from '../pages/EditListingPage';
import { ProtectedRoute } from '../components/ProtectedRoute';

export function App() {
  return (
    <AuthProvider>
      <Routes>
        {/* Public routes with layout */}
        <Route element={<Layout><Routes><Route path="*" element={null} /></Routes></Layout>}>
          <Route path="/" element={<HomePage />} />
          <Route path="/vehicle/:id" element={<VehicleDetailPage />} />
          <Route path="/compare" element={<ComparePage />} />
        <Route
          path="/calculators"
          element={
            <div className="section container-custom">
              <div className="card text-center py-12">
                <h2 className="text-3xl font-display font-bold text-gradient mb-4">
                  Calculators
                </h2>
                <p className="text-gray-400">Coming soon...</p>
              </div>
            </div>
          }
        />
          <Route
            path="/about"
            element={
              <div className="section container-custom">
                <div className="card text-center py-12">
                  <h2 className="text-3xl font-display font-bold text-gradient mb-4">
                    About EV Market
                  </h2>
                  <p className="text-gray-400">
                    Your comprehensive guide to electric vehicles in the UK
                  </p>
                </div>
              </div>
            }
          />
        </Route>

        {/* Auth routes (no layout) */}
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />

        {/* Protected dashboard routes */}
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <DashboardLayout />
            </ProtectedRoute>
          }
        >
          <Route index element={<DashboardOverviewPage />} />
          <Route path="listings" element={<ListingsPage />} />
          <Route path="listings/new" element={<CreateListingPage />} />
          <Route path="listings/edit/:id" element={<EditListingPage />} />
          <Route path="subscription" element={<SubscriptionPage />} />
          <Route path="payments" element={<div className="card"><h2 className="text-2xl font-bold text-white">Payment History</h2><p className="text-gray-400 mt-2">Coming soon...</p></div>} />
        </Route>
      </Routes>
    </AuthProvider>
  );
}

export default App;
