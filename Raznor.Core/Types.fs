namespace Raznor.Core

module Types =
    open System
    open LiteDB

    type SongRecord =
        { [<BsonId(true)>]
          id: ObjectId
          name: string
          path: string
          belongsTo: ObjectId
          createdAt: DateTime }

    type LoopState =
        | Off
        | All
        | Single
