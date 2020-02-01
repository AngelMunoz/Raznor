namespace Raznor.Core



module Types =
    open System
    open LiteDB

    type CollectionSettings =
        { [<BsonId(true)>]
          id: ObjectId
          path: string
          name: string
          createdAt: DateTime }

    type SongRecord =
        { [<BsonId(true)>]
          id: ObjectId
          name: string
          path: string
          belongsTo: ObjectId
          createdAt: DateTime }

    type MusicCollection =
        { [<BsonId(true)>]
          id: ObjectId
          name: string
          createdAt: DateTime
          updatedAt: DateTime }
