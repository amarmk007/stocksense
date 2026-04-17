export default function RecommendationCard({ item }: { item: any }) {
  return (
    <div className="bg-gray-800 rounded-xl p-4">
      <p className="text-white font-bold">{item?.ticker}</p>
    </div>
  )
}
