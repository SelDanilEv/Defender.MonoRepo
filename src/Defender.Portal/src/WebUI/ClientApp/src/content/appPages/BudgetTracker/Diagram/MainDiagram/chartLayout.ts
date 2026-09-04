type MainDiagramLayoutOptions = {
  isMobile: boolean;
  isLargeScreen: boolean;
  additionalLegendMargin: number;
};

export const getMainDiagramLayout = ({
  isMobile,
  isLargeScreen,
  additionalLegendMargin,
}: MainDiagramLayoutOptions) => ({
  height: (isLargeScreen ? 700 : isMobile ? 400 : 450) + additionalLegendMargin,
  margin: {
    top: isLargeScreen ? 10 : 0,
    bottom: isLargeScreen ? 65 + additionalLegendMargin : 0,
    left: isLargeScreen ? 68 : 0,
    right: isLargeScreen ? 40 : 0,
  },
});
