import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getMovie, getEpisodes, getStreamUrl } from '../services/api';

function SeriesDetail() {
  const { id } = useParams();
  const [series, setSeries] = useState(null);
  const [episodes, setEpisodes] = useState([]);

  useEffect(() => {
    getMovie(id).then(res => setSeries(res.data));
    getEpisodes(id).then(res => setEpisodes(res.data));
  }, [id]);

  if (!series) return null;

  const seasons = [...new Set(episodes.map(e => e.seasonNumber))].sort();

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <Link to="/" className="text-gray-400 hover:text-white">← Back to Library</Link>
        <Link to={`/add?parentId=${id}`} className="bg-red-600 hover:bg-red-700 px-4 py-2 rounded-lg text-sm font-medium transition">
          + Add Episode
        </Link>
      </div>
      <h1 className="text-3xl font-bold mb-2">{series.title}</h1>
      {series.description && <p className="text-gray-400 mb-6">{series.description}</p>}

      {seasons.map(season => (
        <div key={season} className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-red-400">Season {season}</h2>
          <div className="space-y-3">
            {episodes
              .filter(e => e.seasonNumber === season)
              .sort((a, b) => a.episodeNumber - b.episodeNumber)
              .map(ep => (
                <Link
                  key={ep.id}
                  to={`/watch/${ep.id}`}
                  className="flex items-center gap-4 bg-gray-800 hover:bg-gray-700 rounded-lg p-4 transition"
                >
                  <span className="text-red-500 font-bold text-lg">S{ep.seasonNumber}E{ep.episodeNumber}</span>
                  <div className="flex-1">
                    <p className="font-medium">{ep.title}</p>
                    {ep.description && <p className="text-sm text-gray-400 truncate">{ep.description}</p>}
                  </div>
                  <span className="text-gray-500">▶</span>
                </Link>
              ))}
          </div>
        </div>
      ))}
    </div>
  );
}

export default SeriesDetail;
