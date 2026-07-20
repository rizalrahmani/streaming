import { useEffect, useState } from 'react';
import { getMovies, deleteMovie, updateMovie } from '../services/api';
import { Link } from 'react-router-dom';
import { useToast } from '../components/Toast';

function FileSourceBadge({ movie }) {
  if (movie.googleDriveFileId) {
    return <span className="text-xs bg-blue-600/50 text-blue-200 px-2 py-0.5 rounded-full">☁ Drive</span>;
  }
  return null;
}

function EditMovieModal({ movie, onClose, onSave }) {
  const [form, setForm] = useState({
    id: movie.id,
    title: movie.title,
    description: movie.description || '',
    genre: movie.genre || '',
    releaseYear: movie.releaseYear,
    googleDriveFileId: movie.googleDriveFileId || '',
    isSeries: movie.isSeries,
    parentId: movie.parentId,
    seasonNumber: movie.seasonNumber,
    episodeNumber: movie.episodeNumber,
  });
  const [saving, setSaving] = useState(false);
  const toast = useToast();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    try {
      await updateMovie(form);
      onSave({ ...movie, ...form });
    } catch (err) {
      toast.error('Failed to save: ' + (err.response?.data?.error || err.message));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/70 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-gray-800 rounded-2xl max-w-lg w-full p-6 shadow-2xl" onClick={e => e.stopPropagation()}>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-2xl font-bold">Edit {movie.isSeries ? 'Series' : 'Movie'}</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-white text-2xl leading-none">×</button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="text-sm text-gray-400 mb-1 block">Title</label>
            <input
              value={form.title}
              onChange={e => setForm({ ...form, title: e.target.value })}
              required
              className="w-full bg-gray-900 border border-gray-700 rounded-lg px-4 py-2 focus:outline-none focus:border-red-500"
            />
          </div>

          <div>
            <label className="text-sm text-gray-400 mb-1 block">Description</label>
            <textarea
              value={form.description}
              onChange={e => setForm({ ...form, description: e.target.value })}
              rows={3}
              className="w-full bg-gray-900 border border-gray-700 rounded-lg px-4 py-2 focus:outline-none focus:border-red-500"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-sm text-gray-400 mb-1 block">Genre</label>
              <input
                value={form.genre}
                onChange={e => setForm({ ...form, genre: e.target.value })}
                placeholder="Action, Drama, etc."
                className="w-full bg-gray-900 border border-gray-700 rounded-lg px-4 py-2 focus:outline-none focus:border-red-500"
              />
            </div>
            <div>
              <label className="text-sm text-gray-400 mb-1 block">Year</label>
              <input
                type="number"
                value={form.releaseYear}
                onChange={e => setForm({ ...form, releaseYear: parseInt(e.target.value) || 0 })}
                className="w-full bg-gray-900 border border-gray-700 rounded-lg px-4 py-2 focus:outline-none focus:border-red-500"
              />
            </div>
          </div>

          {movie.parentId && (
            <div className="bg-gray-900 rounded-lg p-3 text-sm text-gray-400">
              <p>📺 Episode {movie.episodeNumber} (Season {movie.seasonNumber})</p>
              <p className="text-xs text-gray-500 mt-1">Season/episode numbers are managed by the Google Drive folder import.</p>
            </div>
          )}

          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 bg-gray-700 hover:bg-gray-600 py-2.5 rounded-lg font-medium transition"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={saving}
              className="flex-1 bg-red-600 hover:bg-red-700 disabled:bg-gray-600 py-2.5 rounded-lg font-medium transition"
            >
              {saving ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function MovieList() {
  const [movies, setMovies] = useState([]);
  const [search, setSearch] = useState('');
  const [editingMovie, setEditingMovie] = useState(null);

  useEffect(() => {
    getMovies().then(res => setMovies(res.data));
  }, []);

  const filtered = movies.filter(m =>
    m.title.toLowerCase().includes(search.toLowerCase()) ||
    m.genre?.toLowerCase().includes(search.toLowerCase())
  );

  const handleDelete = async (id) => {
    if (!window.confirm('Delete this movie?')) return;
    await deleteMovie(id);
    setMovies(prev => prev.filter(m => m.id !== id));
  };

  const handleSave = (updated) => {
    setMovies(prev => prev.map(m => m.id === updated.id ? updated : m));
    setEditingMovie(null);
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-3xl font-bold">My Library</h1>
        <div className="flex items-center gap-3">
          <input
            type="text"
            placeholder="Search movies..."
            value={search}
            onChange={e => setSearch(e.target.value)}
            className="bg-gray-800 border border-gray-700 rounded-lg px-4 py-2 w-64 focus:outline-none focus:border-red-500"
          />
        </div>
      </div>

      {filtered.length === 0 ? (
        <p className="text-gray-500 text-center mt-20">
          {search ? 'No movies match your search.' : 'No movies yet. Click + Add Movie to get started.'}
        </p>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
          {filtered.map(movie => (
            <div key={movie.id} className="bg-gray-800 rounded-xl overflow-hidden shadow-lg hover:shadow-2xl hover:scale-[1.02] transition">
              <Link to={movie.isSeries ? `/series/${movie.id}` : `/watch/${movie.id}`}>
                <div className="aspect-video bg-gray-700 flex items-center justify-center text-gray-500 hover:bg-gray-600 transition cursor-pointer">
                  {movie.isSeries ? <span className="text-2xl">📺 Series</span> : <span className="text-2xl">🎥 No Cover</span>}
                </div>
              </Link>
              <div className="p-4">
                <h3 className="text-lg font-semibold truncate flex items-center gap-2">
                  {movie.title}
                  <FileSourceBadge movie={movie} />
                </h3>
                <p className="text-sm text-gray-400 mb-3">{movie.genre || '—'} • {movie.releaseYear}</p>
                <div className="flex gap-2">
                  <Link to={movie.isSeries ? `/series/${movie.id}` : `/watch/${movie.id}`} className="flex-1 text-center bg-red-600 hover:bg-red-700 py-2 rounded-lg text-sm font-medium transition">
                    {movie.isSeries ? '📺 Episodes' : '▶ Play'}
                  </Link>
                  <button
                    onClick={() => setEditingMovie(movie)}
                    title="Edit"
                    className="px-3 py-2 bg-gray-700 hover:bg-blue-600 rounded-lg text-sm transition"
                  >
                    ✏️
                  </button>
                  <button
                    onClick={() => handleDelete(movie.id)}
                    title="Delete"
                    className="px-3 py-2 bg-gray-700 hover:bg-red-600 rounded-lg text-sm transition"
                  >
                    🗑
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {editingMovie && (
        <EditMovieModal
          movie={editingMovie}
          onClose={() => setEditingMovie(null)}
          onSave={handleSave}
        />
      )}
    </div>
  );
}

export default MovieList;