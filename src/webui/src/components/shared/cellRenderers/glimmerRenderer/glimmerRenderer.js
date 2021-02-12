// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { svgs } from "utilities";
import { Svg } from "components/shared/svg/svg";

import "../cellRenderer.scss";

export const GlimmerRenderer = (props) =>
    props.value ? <Svg src={svgs.glimmer} className="glimmer-icon" /> : null;
