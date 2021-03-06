﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Visualization.Controls.Bitmap
{
    // TODO Separate calculation from visualization.
    public sealed class FractionBitmap
    {
        public void Create(string filename, Dictionary<string, uint> workByDevelopers,
                           ColorScheme colorMapperMapping, bool legend)
        {
            double allWork = workByDevelopers.Values.Sum(w => w);

            // For the fractal
            var width = 200;
            var height = 200;

            var remainingWidth = width;
            var remainingHeight = height;

            // Reserve plenty of space. Trimmed later.
            var bitmap = new System.Drawing.Bitmap(2000, 2000);
            var graphics = Graphics.FromImage(bitmap);

            var sorted = workByDevelopers.ToList().OrderByDescending(pair => pair.Value).ToList();

            var oneUnitOfWork = width * height / allWork;
            var x = 0;
            var y = 0;

            var vertical = true;

            var index = 0;
            foreach (var developersWork in sorted)
            {
                var brush = colorMapperMapping.GetBrush(developersWork.Key);

                if (legend)
                {
                    var legendY = index * 30;
                    var legendX = 250;
                    graphics.DrawString(developersWork.Key, new Font(FontFamily.GenericSansSerif, 12), Brushes.Black,
                                        legendX + 25, legendY);
                    graphics.FillRectangle(brush, legendX, legendY, 20, 20);
                }

                var workArea = developersWork.Value;

                var pixelArea = oneUnitOfWork * workArea;
                if (index == sorted.Count - 1)
                {
                    // Due to rounding there is always some pixels left. Give the last
                    // developer the remaining space.
                    pixelArea = remainingWidth * remainingHeight;
                }

                if (vertical)
                {
                    var widthOfWork = (int) Math.Round(pixelArea / remainingHeight);
                    graphics.FillRectangle(brush, x, y, widthOfWork, remainingHeight);

                    graphics.DrawRectangle(Pens.Black, x, y, widthOfWork, remainingHeight);

                    x += widthOfWork;
                    remainingWidth -= widthOfWork;
                }
                else
                {
                    var heightOfWork = (int) Math.Round(pixelArea / remainingWidth);
                    graphics.FillRectangle(brush, x, y, remainingWidth, heightOfWork);

                    graphics.DrawRectangle(Pens.Black, x, y, remainingWidth, heightOfWork);

                    y += heightOfWork;
                    remainingHeight -= heightOfWork;
                }

                // Toggle next orientation
                vertical = !vertical;

                index++;
            }

            graphics.DrawRectangle(Pens.Black, 0, 0, width - 1, height - 1);

            BitmapManipulation.TrimBitmap(bitmap).Save(filename);
        }
    }
}