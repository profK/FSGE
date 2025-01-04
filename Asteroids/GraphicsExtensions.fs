module Asteroids.GraphicsExtensions

open Graphics2D


type AnimatedImage = {
    image: Image
    FrameWidth: int
    FrameHeight: int
    FrameCount: int
    FrameTime: float
    CurrentFrame: int
    TimeSinceLastFrame: float
    SubImage: Image
}

module AnimatedImage =
    let create image frameWidth frameHeight frameCount frameTime =
        {
            image = image
            FrameWidth = frameWidth
            FrameHeight = frameHeight
            FrameCount = frameCount
            FrameTime = frameTime
            CurrentFrame = 0
            TimeSinceLastFrame = 0.0
            SubImage = Window.CreateSubImage image 0u 0u (uint32 frameWidth) (uint32 frameHeight)
        }

    let update (dt: float) (image: AnimatedImage) =
        let time = image.TimeSinceLastFrame + dt
        if time > image.FrameTime then

                let currentFrame = (image.CurrentFrame + 1) % image.FrameCount
                let xpos = uint32 ((currentFrame * image.FrameWidth) % image.image.Size.Width)
                let ypos = uint32 ((currentFrame * image.FrameWidth) / image.image.Size.Width)
                
                { image with
                    CurrentFrame = currentFrame
                    TimeSinceLastFrame = 0.0
                    SubImage =
                        Window.CreateSubImage image.image
                            xpos ypos (uint32 image.FrameWidth) (uint32 image.FrameHeight)
                       
                }
        else
            {
                image with
                    TimeSinceLastFrame = time
            }

    let draw matrix (anim: AnimatedImage)=
        Window.DrawImage anim.SubImage matrix
        