namespace Raznor.Core

module Types =
    open System

    type SongRecord =
        { id: Guid
          name: string
          path: string
          createdAt: DateTime }

    type LoopState =
        | Off
        | All
        | Single

    type PlayDirection =
        | Next
        | Previous
        | Direct of SongRecord

    type PlayState =
        | Play
        | Pause
        | Stop

    type PlayerState =
        { length: int64
          song: SongRecord option
          sliderPos: int
          state: PlayState
          loopState: LoopState }

        static member Empty =
            { length = 0
              song = None
              sliderPos = 0
              state = PlayState.Stop
              loopState = LoopState.All }