import { Route, Routes } from 'react-router-dom';
import { Layout } from '../components/layout';
import { HomePage } from '../pages/HomePage';

export function App() {
  return (
    <Layout>
      <Routes>
        <Route path="/" element={<HomePage />} />
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
          path="/compare"
          element={
            <div className="section container-custom">
              <div className="card text-center py-12">
                <h2 className="text-3xl font-display font-bold text-gradient mb-4">
                  Compare EVs
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
        <Route
          path="/vehicle/:id"
          element={
            <div className="section container-custom">
              <div className="card text-center py-12">
                <h2 className="text-3xl font-display font-bold text-gradient mb-4">
                  Vehicle Details
                </h2>
                <p className="text-gray-400">Coming soon...</p>
              </div>
            </div>
          }
        />
      </Routes>
    </Layout>
  );
}

export default App;
