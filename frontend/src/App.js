import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import { ToastProvider } from './components/Toast';
import MovieList from './pages/MovieList';
import AddMovie from './pages/AddMovie';
import WatchMovie from './pages/WatchMovie';
import SeriesDetail from './pages/SeriesDetail';
import GoogleDriveImport from './pages/GoogleDriveImport';

function App() {
  return (
    <BrowserRouter>
      <ToastProvider>
      <div className="min-h-screen bg-gray-900 text-white">
        <nav className="bg-gray-800 border-b border-gray-700 px-6 py-4 flex items-center justify-between">
          <Link to="/" className="text-2xl font-bold text-red-500">StreamMass</Link>
          <div className="flex items-center gap-3">
            <Link to="/google-drive" className="bg-blue-600 hover:bg-blue-700 px-4 py-2 rounded-lg text-sm font-medium transition">
              ☁ Google Drive
            </Link>
            <Link to="/add" className="bg-red-600 hover:bg-red-700 px-4 py-2 rounded-lg text-sm font-medium transition">
              + Add Movie
            </Link>
          </div>
        </nav>
        <main className="p-6">
          <Routes>
            <Route path="/" element={<MovieList />} />
            <Route path="/add" element={<AddMovie />} />
            <Route path="/watch/:id" element={<WatchMovie />} />
            <Route path="/series/:id" element={<SeriesDetail />} />
            <Route path="/google-drive" element={<GoogleDriveImport />} />
          </Routes>
        </main>
      </div>
      </ToastProvider>
    </BrowserRouter>
  );
}

export default App;