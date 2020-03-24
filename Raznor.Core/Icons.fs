namespace Raznor.Core

[<RequireQualifiedAccess>]
module Icons =
    open Avalonia.Controls
    open Avalonia.Controls.Shapes
    open Avalonia.FuncUI.DSL


    let shuffle =
        Canvas.create
            [ Canvas.width 24.0
              Canvas.height 24.0
              Canvas.children
                  [ Path.create
                      [ Path.fill "black"
                        Path.data
                            "M17,3L22.25,7.5L17,12L22.25,16.5L17,21V18H14.26L11.44,15.18L13.56,13.06L15.5,15H17V12L17,9H15.5L6.5,18H2V15H5.26L14.26,6H17V3M2,6H6.5L9.32,8.82L7.2,10.94L5.26,9H2V6Z" ] ] ]

    let repeat =
        Canvas.create
            [ Canvas.width 24.0
              Canvas.height 24.0
              Canvas.children
                  [ Path.create
                      [ Path.fill "black"
                        Path.data "M17,17H7V14L3,18L7,22V19H19V13H17M7,7H17V10L21,6L17,2V5H5V11H7V7Z" ] ] ]

    let repeatOne =
        Canvas.create
            [ Canvas.width 24.0
              Canvas.height 24.0
              Canvas.children
                  [ Path.create
                      [ Path.fill "black"
                        Path.data
                            "M13,15V9H12L10,10V11H11.5V15M17,17H7V14L3,18L7,22V19H19V13H17M7,7H17V10L21,6L17,2V5H5V11H7V7Z" ] ] ]

    let repeatOff =
        Canvas.create
            [ Canvas.width 24.0
              Canvas.height 24.0
              Canvas.children
                  [ Path.create
                      [ Path.fill "black"
                        Path.data
                            "M2,5.27L3.28,4L20,20.72L18.73,22L15.73,19H7V22L3,18L7,14V17H13.73L7,10.27V11H5V8.27L2,5.27M17,13H19V17.18L17,15.18V13M17,5V2L21,6L17,10V7H8.82L6.82,5H17Z" ] ] ]

    let stop =
        Canvas.create
            [ Canvas.width 24.0
              Canvas.height 24.0
              Canvas.children
                  [ Path.create
                      [ Path.fill "black"
                        Path.data "M18,18H6V6H18V18Z" ] ] ]

    let play =
        Canvas.create
            [ Canvas.width 24.0
              Canvas.height 24.0
              Canvas.children
                  [ Path.create
                      [ Path.fill "black"
                        Path.data "M8,5.14V19.14L19,12.14L8,5.14Z" ] ] ]

    let pause =
        Canvas.create
            [ Canvas.width 24.0
              Canvas.height 24.0
              Canvas.children
                  [ Path.create
                      [ Path.fill "black"
                        Path.data "M14,19H18V5H14M6,19H10V5H6V19Z" ] ] ]

    let previous =
        Canvas.create
            [ Canvas.width 24.0
              Canvas.height 24.0
              Canvas.children
                  [ Path.create
                      [ Path.fill "black"
                        Path.data "M6,18V6H8V18H6M9.5,12L18,6V18L9.5,12Z" ] ] ]

    let next =
        Canvas.create
            [ Canvas.width 24.0
              Canvas.height 24.0
              Canvas.children
                  [ Path.create
                      [ Path.fill "black"
                        Path.data "M16,18H18V6H16M6,18L14.5,12L6,6V18Z" ] ] ]

    let cast =
        Canvas.create
            [ Canvas.width 24.0
              Canvas.height 24.0
              Canvas.children
                  [ Path.create
                      [ Path.fill "black"
                        Path.data
                            "M1,10V12A9,9 0 0,1 10,21H12C12,14.92 7.07,10 1,10M1,14V16A5,5 0 0,1 6,21H8A7,7 0 0,0 1,14M1,18V21H4A3,3 0 0,0 1,18M21,3H3C1.89,3 1,3.89 1,5V8H3V5H21V19H14V21H21A2,2 0 0,0 23,19V5C23,3.89 22.1,3 21,3Z" ] ] ]

    let castOff =
        Canvas.create
            [ Canvas.width 24.0
              Canvas.height 24.0
              Canvas.children
                  [ Path.create
                      [ Path.fill "black"
                        Path.data
                            "M1.6,1.27L0.25,2.75L1.41,3.8C1.16,4.13 1,4.55 1,5V8H3V5.23L18.2,19H14V21H20.41L22.31,22.72L23.65,21.24M6.5,3L8.7,5H21V16.14L23,17.95V5C23,3.89 22.1,3 21,3M1,10V12A9,9 0 0,1 10,21H12C12,14.92 7.08,10 1,10M1,14V16A5,5 0 0,1 6,21H8A7,7 0 0,0 1,14M1,18V21H4A3,3 0 0,0 1,18Z" ] ] ]
