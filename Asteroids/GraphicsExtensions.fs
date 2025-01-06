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
    Loop: bool
    IsPlaying: bool
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
            Loop = false
            IsPlaying = false
            SubImage = Window.CreateSubImage image 0u 0u (uint32 frameWidth) (uint32 frameHeight)
        }
    let start (image: AnimatedImage) =
        { image with
            IsPlaying = true
            TimeSinceLastFrame = 0
            CurrentFrame = 0 
        }

    let update (dt: float) (image: AnimatedImage) =
        let time = image.TimeSinceLastFrame + dt
        if image.IsPlaying then
            if time > image.FrameTime then
                let currentFrame = (image.CurrentFrame + 1) % image.FrameCount
                if image.Loop || (currentFrame <> 0) then
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
                    {image with
                        TimeSinceLastFrame = 0
                        IsPlaying = false
                        CurrentFrame = 0 
                    }
            else
                {image with
                        TimeSinceLastFrame = time
                    }
        else           
            {
                image with
                    TimeSinceLastFrame = time
            }

    let draw matrix (anim: AnimatedImage)=
        Window.DrawImage anim.SubImage matrix
        