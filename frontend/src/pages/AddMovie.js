import { useState, useEffect } from 'react';
import { createMovie, getMovies, getMovie } from '../services/api';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { useToast } from '../components/Toast';

function AddMovie() {
  const [searchParams] = useSearchParams();
  const parentId = searchParams.get('parentId') || '';
  const navigate = useNavigate();

  const [form, setForm] = useState({
    title: '', description: '', genre: '', releaseYear: '',
    googleDriveFileId: '', isSeries: false, parentId, seasonNumber: '', episodeNumber: ''
  });
  const [seriesList, setSeriesList] = useState([]);
  const toast = useToast();

  const isEpisodeMode = !!parentId;

  useEffect(() => {
    if (isEpisodeMode) {
      getMovie(parentId).then(res => {
        setForm(prev => ({
          ...prev,
          genre: res.data.genre || '',
          releaseYear: res.data.releaseYear || '',
        }));
      });
    } else {
      getMovies().then(res => setSeriesList(res.data.filter(m => m.isSeries)));
    }
  }, [parentId, isEpisodeMode]);

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!form.isSeries && !form.googleDriveFileId) {
      toast.warning('Please provide a Google Drive File ID');
      return;
    }

    const payload = {
      title: isEpisodeMode ? `S${form.seasonNumber}E${form.episodeNumber} - ${form.title || 'Episode'}` : form.title,
      description: form.description || null,
      genre: form.genre || null,
      releaseYear: parseInt(form.releaseYear) || new Date().getFullYear(),
      googleDriveFileId: form.googleDriveFileId || null,
      isSeries: form.isSeries,
      parentId: isEpisodeMode ? parseInt(parentId) : (form.parentId ? parseInt(form.parentId) : null),
      seasonNumber: form.seasonNumber ? parseInt(form.seasonNumber) : null,
      episodeNumber: form.episodeNumber ? parseInt(form.episodeNumber) : null,
    };
    await createMovie(payload);
    navigate(isEpisodeMode ? `/series/${parentId}` : '/');
  };

  if (isEpisodeMode) {
    return (
      <div className="max-w-lg mx-auto mt-10">
        <Link to={`/series/${parentId}`} className="bg-gray-700 hover:bg-gray-600 px-4 py-2 rounded-lg text-sm transition inline-block mb-6">← Back to Series</Link>
        <h1 className="text-3xl font-bold mb-6">Add Episode</h1>
        <form onSubmit={handleSubmit} className="space-y-4">
          <input placeholder="Episode Title (optional)" value={form.title} onChange={e => setForm({ ...form, title: e.target.value })}
            className="w-full bg-gray-800 border border-gray-700 rounded-lg px-4 py-3 focus:outline-none focus:border-red-500" />
          <div className="flex gap-4">
            <input type="number" placeholder="Season" value={form.seasonNumber} onChange={e => setForm({ ...form, seasonNumber: e.target.value })} required
              className="flex-1 bg-gray-800 border border-gray-700 rounded-lg px-4 py-3 focus:outline-none focus:border-red-500" />
            <input type="number" placeholder="Episode" value={form.episodeNumber} onChange={e => setForm({ ...form, episodeNumber: e.target.value })} required
              className="flex-1 bg-gray-800 border border-gray-700 rounded-lg px-4 py-3 focus:outline-none focus:border-red-500" />
          </div>
          <input placeholder="Google Drive File ID" value={form.googleDriveFileId} onChange={e => setForm({ ...form, googleDriveFileId: e.target.value })} required
            className="w-full bg-gray-800 border border-gray-700 rounded-lg px-4 py-3 focus:outline-none focus:border-blue-500" />
          <button type="submit" className="w-full bg-red-600 hover:bg-red-700 py-3 rounded-lg font-semibold transition">
            Save Episode
          </button>
        </form>
      </div>
    );
  }

  return (
    <div className="max-w-lg mx-auto mt-10">
      <Link to="/" className="bg-gray-700 hover:bg-gray-600 px-4 py-2 rounded-lg text-sm transition inline-block mb-6">← Back</Link>
      <h1 className="text-3xl font-bold mb-6">Add {form.isSeries ? 'Series' : 'Movie'}</h1>
      <form onSubmit={handleSubmit} className="space-y-4">
        <input placeholder="Title" value={form.title} onChange={e => setForm({ ...form, title: e.target.value })} required
          className="w-full bg-gray-800 border border-gray-700 rounded-lg px-4 py-3 focus:outline-none focus:border-red-500" />
        <textarea placeholder="Description" value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} rows={3}
          className="w-full bg-gray-800 border border-gray-700 rounded-lg px-4 py-3 focus:outline-none focus:border-red-500" />
        <div className="flex gap-4">
          <input placeholder="Genre" value={form.genre} onChange={e => setForm({ ...form, genre: e.target.value })}
            className="flex-1 bg-gray-800 border border-gray-700 rounded-lg px-4 py-3 focus:outline-none focus:border-red-500" />
          <input type="number" placeholder="Year" value={form.releaseYear} onChange={e => setForm({ ...form, releaseYear: e.target.value })}
            className="w-28 bg-gray-800 border border-gray-700 rounded-lg px-4 py-3 focus:outline-none focus:border-red-500" />
        </div>

        <label className="flex items-center gap-3 cursor-pointer">
          <input type="checkbox" checked={form.isSeries} onChange={e => setForm({ ...form, isSeries: e.target.checked, parentId: '', seasonNumber: '', episodeNumber: '' })}
            className="w-5 h-5 accent-red-500" />
          <span className="text-gray-300">This is a Series (no video file, just metadata)</span>
        </label>

        {!form.isSeries && (
          <>
            <select value={form.parentId} onChange={e => setForm({ ...form, parentId: e.target.value })}
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-4 py-3 focus:outline-none focus:border-red-500">
              <option value="">Not part of a series</option>
              {seriesList.map(s => (
                <option key={s.id} value={s.id}>{s.title}</option>
              ))}
            </select>

            {form.parentId && (
              <div className="flex gap-4">
                <input type="number" placeholder="Season" value={form.seasonNumber} onChange={e => setForm({ ...form, seasonNumber: e.target.value })}
                  className="flex-1 bg-gray-800 border border-gray-700 rounded-lg px-4 py-3 focus:outline-none focus:border-red-500" />
                <input type="number" placeholder="Episode" value={form.episodeNumber} onChange={e => setForm({ ...form, episodeNumber: e.target.value })}
                  className="flex-1 bg-gray-800 border border-gray-700 rounded-lg px-4 py-3 focus:outline-none focus:border-red-500" />
              </div>
            )}

            <input placeholder="Google Drive File ID" value={form.googleDriveFileId} onChange={e => setForm({ ...form, googleDriveFileId: e.target.value })} required
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-4 py-3 focus:outline-none focus:border-blue-500" />
          </>
        )}

        <button type="submit" className="w-full bg-red-600 hover:bg-red-700 py-3 rounded-lg font-semibold transition">
          Save {form.isSeries ? 'Series' : 'Movie'}
        </button>
      </form>
    </div>
  );
}

export default AddMovie;