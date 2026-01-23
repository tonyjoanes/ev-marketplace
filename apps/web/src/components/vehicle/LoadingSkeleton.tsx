export function LoadingSkeleton() {
  return (
    <div className="card animate-pulse">
      <div className="flex items-start justify-between mb-4">
        <div className="flex-1">
          <div className="h-4 w-20 bg-dark-700 rounded mb-2" />
          <div className="h-6 w-40 bg-dark-700 rounded" />
        </div>
        <div className="h-6 w-16 bg-dark-700 rounded-full" />
      </div>

      <div className="grid grid-cols-2 gap-4 mb-6">
        {[...Array(4)].map((_, i) => (
          <div key={i} className="space-y-2">
            <div className="h-3 w-16 bg-dark-700 rounded" />
            <div className="h-5 w-20 bg-dark-700 rounded" />
          </div>
        ))}
      </div>

      <div className="flex gap-2 mb-6">
        {[...Array(3)].map((_, i) => (
          <div key={i} className="h-6 w-16 bg-dark-700 rounded-full" />
        ))}
      </div>

      <div className="mb-6">
        <div className="h-8 w-32 bg-dark-700 rounded mb-1" />
        <div className="h-4 w-24 bg-dark-700 rounded" />
      </div>

      <div className="flex gap-2">
        <div className="h-12 flex-1 bg-dark-700 rounded-lg" />
        <div className="h-12 w-12 bg-dark-700 rounded-lg" />
      </div>
    </div>
  );
}

export default LoadingSkeleton;
