import { useParams, Link } from 'react-router-dom';
import { getStreamUrl, getMovie } from '../services/api';
import { useState, useEffect } from 'react';

function WatchMovie() {
  const { id } = useParams();
  const [movie, setMovie] = useState(null);

  useEffect(() => {
    getMovie(id).then(res => setMovie(res.data));
  }, [id]);

  const streamUrl = getStreamUrl(id);
  const downloadUrl = streamUrl;

  return (
    <div className="fixed inset-0 bg-black flex flex-col">
      <div className="flex items-center justify-between px-6 py-4 bg-gray-900/90">
        <Link to="/" className="text-2xl font-bold text-red-500">StreamMass</Link>
        <Link to="/" className="bg-gray-700 hover:bg-gray-600 px-4 py-2 rounded-lg text-sm transition">
          ← Back
        </Link>
      </div>

      <div className="bg-blue-900/50 border-b border-blue-700 px-6 py-3 text-blue-200 text-sm text-center">
        <strong>Streamed from Google Drive.</strong> Browser may not play all video formats.
        <div className="mt-2 flex justify-center gap-3">
          <a href={downloadUrl} download
            className="bg-blue-600 hover:bg-blue-700 px-4 py-1.5 rounded text-sm font-medium transition">
            ⬇ Download
          </a>
          <span className="text-blue-400 py-1.5 text-sm">
            • Open in VLC app on your phone
          </span>
        </div>
      </div>

      <div className="flex-1 flex items-center justify-center p-4">
        <video controls autoPlay className="max-w-full max-h-full rounded-lg" style={{ maxHeight: '85vh' }}>
          <source src={streamUrl} type="video/mp4" />
          <p className="text-gray-400 text-center p-4">
            Your browser can't play this video.
            <br />
            <a href={downloadUrl} download className="text-red-500 underline">Download</a> and play with VLC.
          </p>
        </video>
      </div>
    </div>
  );
}

export default WatchMovie;